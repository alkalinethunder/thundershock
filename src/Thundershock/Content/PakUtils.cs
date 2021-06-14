using System;
using System.IO;
using System.Linq;
using System.Text;
using BinaryPack;

namespace Thundershock.Content
{
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
            using var file = File.OpenWrite(pakDestination);
            
            // Create the root directory node.
            var rootDirectory = new PakDirectory
            {
                Name = "/",
                DirectoryType = PakDirectoryType.Root
            };
            
            // Memory stream for file contents.
            using var pakStream = new MemoryStream();
            
            // Recurse through the file system and build the pak directory tree. When
            // this method returns, memStream will be filled with a contiguous blob of
            // every single file's contents.
            GatherPakContents(rootDirectory, pakStream, sourceDirectory);
            
            // Now we can start writing the pak file.
            // Let's use BinaryWriter to help.
            using var writer = new BinaryWriter(file, Encoding.UTF8);
            
            // Start by writing the header.
            writer.Write(PakMagic);
            
            // Now we can write the version code. We'll use 1.0 for now.
            writer.Write(1);
            
            // Use BinaryPack to serialize the directory tree.
            using (var memStream = new MemoryStream())
            {
                BinaryConverter.Serialize<PakDirectory>(rootDirectory, memStream);

                memStream.Seek(0, SeekOrigin.Begin);
                
                // Write the stream length so we know how long the tree is.
                writer.Write(memStream.Length);
                
                // CopyTo will help us out here.
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
            pakStream.CopyTo(file);
            
            // And we're done.
        }

        private static void GatherPakContents(PakDirectory folderOrRoot, Stream pakData, string sourceDirectory)
        {
            // Go through each file in the source directory.
            foreach (var file in Directory.GetFiles(sourceDirectory))
            {
                // Get the file name we're going to use for the directory entry.
                // We're going to omit the extension in the Pak file for consistency between
                // the old MGCB content system and the Pak content system. Less legacy code to
                // rewrite that way.
                var fname = Path.GetFileNameWithoutExtension(file);
                
                // Open the file so we can read its data.
                var fileStream = File.OpenRead(file);
                
                // File length. We'll need this.
                var fileLength = fileStream.Length;
                
                // Current position of the pak data stream.
                var dataStart = pakData.Position;
                
                // Create the file entry. We have all the info we need for that.
                var fileEntry = new PakDirectory
                {
                    Name = fname,
                    DirectoryType = PakDirectoryType.File,
                    DataStart = dataStart,
                    DataLength = fileLength
                };
                
                // Add the child to its parent folder.
                folderOrRoot.Children.Add(fileEntry);
                
                // Now let's start reading the data from the file into the pak data stream.
                // I would use CopyTo for this but doing it manually means we can log the progress.
                var block = new byte[2048];
                while (fileLength > 0)
                {
                    var readCount = (int) Math.Min(fileLength, block.Length);

                    fileStream.Read(block, 0, readCount);
                    
                    pakData.Write(block, 0, readCount);

                    fileLength -= readCount;
                }
            }
            
            // Create child folders and gather their contents.
            foreach (var childFolder in Directory.GetDirectories(sourceDirectory))
            {
                // Create the child entry with matching file name.
                var dname = Path.GetFileName(childFolder);
                var childEntry = new PakDirectory
                {
                    Name = dname,
                    DirectoryType = PakDirectoryType.Folder
                };
                
                // Add child to its parent directory.
                folderOrRoot.Children.Add(childEntry);

                // Gather the child's pak contents.
                GatherPakContents(childEntry, pakData, childFolder);
            }
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

            if (version == 1)
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
                    directoryTree = BinaryConverter.Deserialize<PakDirectory>(memStream);
                }

                var currPos = stream.Position;

                var node = new PakFile(path, stream, directoryTree, currPos);

                return node;
            }

            throw new InvalidOperationException("Unsupported Pak File Version.");
        }

    }
}