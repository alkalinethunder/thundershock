using System;
using System.IO;

namespace Thundershock.IO
{
    public class SubmissionMemoryStream : Stream
    {
        private Action<byte[]> _flush;
        private byte[] _bytes;
        private long _pos;

        public SubmissionMemoryStream(byte[] initialBytes, Action<byte[]> flush)
        {
            _bytes = initialBytes;
            _flush = flush;
        }

        public override void Close()
        {
            _flush(_bytes);
            base.Close();
        }

        public override void Flush()
        {
            _flush(_bytes);
        }

        private void ThrowIfOutOfBounds(long offset, long count, long length)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Value must be greater than 0.");

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset,
                    "Offset must be greater than or equal to 0.");

            if (offset + count > length)
                throw new InvalidOperationException("Offset and count exceed the bounds of the array.");

        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            ThrowIfOutOfBounds(offset, count, buffer.Length);

            var read = 0;

            while (count > 0 && Position < Length)
            {
                buffer[offset] = _bytes[Position];
                offset++;
                Position++;
                count--;
                read++;
            }
            
            return read;
        }

        public override long Seek(long offset, SeekOrigin seekOrigin)
        {
            var origin = 0L;
            switch (seekOrigin)
            {
                case SeekOrigin.Begin:
                    origin = 0;
                    break;
                case SeekOrigin.Current:
                    origin = Position;
                    break;
                case SeekOrigin.End:
                    origin = Length;
                    break;
            }

            ThrowIfOutOfBounds(origin, offset, Length);

            return Position;
        }

        public override void SetLength(long value)
        {
            Array.Resize(ref _bytes, (int) value);
            if (Position > Length)
                Position = Length;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            ThrowIfOutOfBounds(offset, count, buffer.Length);

            var len = Position + count;
            if (len >= Length)
            {
                SetLength(len);
            }

            for (var i = offset; i < offset + count; i++)
            {
                _bytes[Position] = buffer[i];
                Position++;
            }
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _bytes.Length;

        public override long Position
        {
            get => _pos;
            set
            {
                if (value < 0 || value > Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _pos = value;
            }
        }
    }
}