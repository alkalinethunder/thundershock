using System;
using System.IO;

namespace Thundershock.Content
{
    public class PakFile : IDisposable
    {
        private long _dataStart;
        private PakDirectory _directoryTree;
        private Stream _pakStream;
        private string _filePath;

        public PakDirectory RootDirectory => _directoryTree;
        public string PakFilePath => _filePath;
        
        public PakFile(string file, Stream stream, PakDirectory directoryTree, long dataStart)
        {
            _dataStart = dataStart;
            _filePath = file;
            _pakStream = stream;
            _directoryTree = directoryTree;
        }

        public Stream LoadData(PakDirectory directory)
        {
            if (_pakStream == null)
                throw new ObjectDisposedException("PakFile");
            
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));

            if (directory.DirectoryType != PakDirectoryType.File)
                throw new InvalidOperationException("Cannot load bitstream data of a PakFile folder.");

            _pakStream.Seek(_dataStart + directory.DataStart, SeekOrigin.Begin);

            var memStream = new MemoryStream();

            var dataLen = directory.DataLength;
            var block = new byte[2048];
            while (dataLen > 0)
            {
                var readCount = (int) Math.Min(block.Length, dataLen);
                _pakStream.Read(block, 0, readCount);
                memStream.Write(block, 0, readCount);
                dataLen -= readCount;
            }

            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }

        public void Dispose()
        {
            _dataStart = 0;
            _directoryTree = null;
            _filePath = null;
            _pakStream?.Dispose();
            _pakStream = null;
        }
    }
}