using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thundershock.Core;
using Thundershock.Core.Debugging;

namespace Thundershock.Content
{
    [CheatAlias("Thunderpak")]
    public static class PakUtils
    {
        private static readonly byte[] PakMagic = Encoding.UTF8.GetBytes("4k1NtHn0r");

        public static void MakePak(string sourceDirectory, string pakDestination)
        {
            // Check to make sure the source directory exists.
            if (!Directory.Exists(sourceDirectory))
            {
                throw new InvalidOperationException("Cannot make a Pak file out of that directory.",
                    new DirectoryNotFoundException(sourceDirectory));
            }

            // Open the destination file.
            Logger.Log("Opening new Pak file: " + pakDestination);
            using var file = File.OpenWrite(pakDestination);
            
            // Create the root directory node.
            Logger.Log("Creating root Pak Directory...");
            var rootDirectory = new PakDirectory
            {
                Name = "/",
                FileName = "/",
                DirectoryType = PakDirectoryType.Root
            };
            
            // Memory stream for file contents.
            Logger.Log("Allocating memory for temporary PakFile bitstream...");
            using (var pakStream = File.OpenWrite(pakDestination + ".raw"))
            {
                // Recurse through the file system and build the pak directory tree. When
                // this method returns, memStream will be filled with a contiguous blob of
                // every single file's contents.
                Logger.Log("Gathering PAK data...");
                GatherPakContents(rootDirectory, pakStream, sourceDirectory);
                Logger.Log("Done gathering Pak data..");
            }

            // Now we can start writing the pak file.
            // Let's use BinaryWriter to help.
            Logger.Log("Preparing to write PakFile to disk...");
            using var writer = new BinaryWriter(file, Encoding.UTF8);
            
            // Start by writing the header.
            Logger.Log("Writing Pak header...");
            writer.Write(PakMagic);
            
            // Now we can write the version code. We'll use 1.0 for now.
            Logger.Log("Writing Pak version...");
            writer.Write(2); // Version 1 = uncompressed, version 2 = gzipped assets.
            
            // Use BinaryPack to serialize the directory tree.
            using (var memStream = new MemoryStream())
            {
                Logger.Log("Serializing directory structure...");
                using (var hWriter = new BinaryWriter(memStream, Encoding.UTF8, true))
                {
                    WriteDirectoryData(rootDirectory, hWriter);
                }

                memStream.Seek(0, SeekOrigin.Begin);
                
                // Write the stream length so we know how long the tree is.
                Logger.Log($"Directory structure data is {memStream.Length} bytes long.");
                writer.Write(memStream.Length);
                
                // CopyTo will help us out here.
                Logger.Log("Writing directory structure information to PakFile...");
                memStream.CopyTo(file);
            }
            
            // The engine will reconstruct the directory tree when mounting the Pak file.
            // The directory tree contains the relative start points of files' data and their byte lengths
            // in the Pak file.
            //
            // The engine will calculate the absolute start of the file data based on the size of the pak
            // header. We don't need to write it.
            //
            // So now we can just write the pak file contents.
            using (var pakStream = File.OpenRead(pakDestination + ".raw"))
            {
                Logger.Log($"PakFile data is {pakStream.Length} bytes in size. Writing to disk...");
                pakStream.CopyTo(file);
            }
            
            // Clean-up
            Logger.Log("Cleaning up...");
            File.Delete(pakDestination + ".raw");
            
            // And we're done.
            Logger.Log($"PakFile {pakDestination} - Written successfully!", LogLevel.Message);
        }

        private static void WriteDirectoryData(PakDirectory directory, BinaryWriter writer)
        {
            writer.Write(directory.Name);
            writer.Write(directory.FileName);
            writer.Write((byte) directory.DirectoryType);

            if (directory.DirectoryType == PakDirectoryType.File)
            {
                writer.Write(directory.DataLength);
                writer.Write(directory.DataStart);
            }
            else
            {
                writer.Write(directory.Children.Count);
                foreach (var child in directory.Children)
                    WriteDirectoryData(child, writer);
            }
        }
        
        private static void GatherPakContents(PakDirectory folderOrRoot, Stream pakData, string sourceDirectory)
        {
            // Go through each file in the source directory.
            Logger.Log($"Gathering files in {sourceDirectory}...");
            foreach (var file in Directory.GetFiles(sourceDirectory))
            {
                // Open the file so we can read its data.
                Logger.Log("Opening " + file + "...");
                var fileStream = File.OpenRead(file);

                // Get the file name we're going to use for the directory entry.
                // We're going to omit the extension in the Pak file for consistency between
                // the old MGCB content system and the Pak content system. Less legacy code to
                // rewrite that way.
                Logger.Log($"Importing {file}...");
                var fname = Path.GetFileNameWithoutExtension(file);
                var withExtension = Path.GetFileName(file);
                
                Logger.Log(" >>> File Name: " + fname);

                if (string.IsNullOrWhiteSpace(fname))
                {
                    Logger.Log("DOTFILE: Skipping an obvious dotfile, these are not supported by the PakFS.", LogLevel.Warning);
                    continue;
                }
                
                // Version 2 PAKs use GZip compression to save disk space. Let's take the file contents
                // and zip them up.
                using (var zipResult = new MemoryStream())
                {
                    // Copy the file data in.
                    using (var zippo =
                        new GZipStream(zipResult, CompressionLevel.Optimal, true))
                    {
                        fileStream.CopyTo(zippo);
                    }
                    
                    // Seek the zip result back to the start.
                    zipResult.Seek(0, SeekOrigin.Begin);

                    // File length. We'll need this.
                    var fileLength = zipResult.Length;
                    Logger.Log($" >>> Data Length (compressed): {fileLength} bytes");

                    // Current position of the pak data stream.
                    var dataStart = pakData.Position;
                    Logger.Log($" >>> Pak Start: d+{dataStart} bytes");

                    // Create the file entry. We have all the info we need for that.
                    Logger.Log("Creating file entry...");
                    var fileEntry = new PakDirectory
                    {
                        Name = fname,
                        FileName = withExtension,
                        DirectoryType = PakDirectoryType.File,
                        DataStart = dataStart,
                        DataLength = fileLength
                    };

                    // Add the child to its parent folder.
                    folderOrRoot.Children.Add(fileEntry);
                    Logger.Log("File entry added to Pak directory.");

                    // Now let's start reading the data from the file into the pak data stream.
                    // I would use CopyTo for this but doing it manually means we can log the progress.
                    Logger.Log("Preparing to copy file data into PakFile...");
                    var block = new byte[8 * 1024 * 1024];
                    var lastProgress = -1d;
                    while (fileLength > 0)
                    {
                        var progress = Math.Round((fileStream.Position / (float) fileStream.Length) * 100);
                        var readCount = (int) Math.Min(fileLength, block.Length);

                        if (lastProgress < progress)
                        {
                            Logger.Log(
                                $" >>> Progress {progress}% - Written: {fileStream.Position} - Left: {fileLength} - Block size: {block.Length} - Next read: {readCount}");
                            lastProgress = progress;
                        }

                        zipResult.Read(block, 0, readCount);
                        pakData.Write(block, 0, readCount);
                        fileLength -= readCount;
                    }
                }

                Logger.Log(" >>> Done.");
            }

            Logger.Log("Files imported successfully.");
            
            // Create child folders and gather their contents.
            Logger.Log("Gathering directories in " + sourceDirectory);
            foreach (var childFolder in Directory.GetDirectories(sourceDirectory))
            {
                // Create the child entry with matching file name.
                Logger.Log("Creating child directory entry for " + childFolder);
                var dname = Path.GetFileName(childFolder);

                if (string.IsNullOrWhiteSpace(dname))
                {
                    Logger.Log("DOTFILE: This directory is a dotfile, skipping.", LogLevel.Warning);
                    continue;
                }
                
                var childEntry = new PakDirectory
                {
                    Name = dname,
                    FileName = dname,
                    DirectoryType = PakDirectoryType.Folder
                };
                
                // Add child to its parent directory.
                Logger.Log("Added child directory " + dname + " to parent directory " + folderOrRoot.Name);
                folderOrRoot.Children.Add(childEntry);

                // Gather the child's pak contents.
                Logger.Log("Gathering child directory data...");
                GatherPakContents(childEntry, pakData, childFolder);
            }
        }

        private static PakDirectory ConstructDirectoryTree(BinaryReader reader)
        {
            var name = reader.ReadString();
            var fname = reader.ReadString();
            var entType = (PakDirectoryType) reader.ReadByte();

            var dir = new PakDirectory
            {
                Name = name,
                FileName = fname,
                DirectoryType = entType
            };

            if (entType == PakDirectoryType.File)
            {
                dir.DataLength = reader.ReadInt64();
                dir.DataStart = reader.ReadInt64();
            }
            else
            {
                var childrenCount = reader.ReadInt32();

                for (var i = 0; i < childrenCount; i++)
                {
                    var child = ConstructDirectoryTree(reader);
                    dir.Children.Add(child);
                }
            }
            
            return dir;
        }
        
        public static PakFile OpenPak(string path)
        {
            var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            // Read the header first.
            var magic = reader.ReadBytes(PakMagic.Length);
            if (!magic.SequenceEqual(PakMagic))
                throw new InvalidOperationException("Unrecognized file type.");

            // Read the pak file version. Not useful right now but future proofing is handy.
            var version = reader.ReadInt32();
            var useCompression = version == 2;
            
            if (version == 1 || version == 2)
            {
                // Now we can read the Directory Tree.
                var directoryTreeSize = reader.ReadInt64();
                var directoryTree = null as PakDirectory;

                using (var memStream = new MemoryStream())
                {
                    while (directoryTreeSize > 0)
                    {
                        var blockSize = Math.Min((int) directoryTreeSize, 2048);

                        var bytes = reader.ReadBytes(blockSize);
                        memStream.Write(bytes, 0, bytes.Length);

                        directoryTreeSize -= blockSize;
                    }

                    memStream.Seek(0, SeekOrigin.Begin);

                    using (var hReader = new BinaryReader(memStream, Encoding.UTF8))
                    {
                        directoryTree = ConstructDirectoryTree(hReader);
                    }
                }

                var currPos = stream.Position;

                var node = new PakFile(path, stream, directoryTree, currPos, useCompression);

                return node;
            }

            throw new InvalidOperationException("Unsupported Pak File Version.");
        }

        [Cheat("MakePak")]
        public static Task MakePakAsync(string source, string dest)
        {
            return Task.Run(() =>
            {
                try
                {
                    MakePak(source, dest);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            });
        }

    }
}