using System;
using System.IO;
using System.Reflection;
using NVorbis;
using Thundershock.Core;

namespace Thundershock.Audio
{
    public class Song : IDisposable
    {
        private IMusicReader _pcmStream;
        private int _sampleRate;
        private int _channels;
        
        public int SampleRate => _sampleRate;
        public int Channels => _channels;

        private Song(IMusicReader pcmStream, int channels, int sampleRate)
        {
            _pcmStream = pcmStream;
            _channels = channels;
            _sampleRate = sampleRate;
        }

        public byte[] ReadFrame()
        {
            return _pcmStream.ReadSamples(_channels * 1024);
        }
        
        public void Dispose()
        {
        }

        public static Song FromOggStream(Stream stream)
        {
            var reader = new VorbisReader(stream, true);

            var vorbisToPCMStream = new VorbisStream(reader);

            return new Song(vorbisToPCMStream, reader.Channels, reader.SampleRate);
        }
        
        public static Song FromOggResource(Assembly ass, string resource)
        {
            if (Resource.GetStream(ass, resource, out var stream))
            {
                return FromOggStream(stream);
            }

            throw new InvalidOperationException("Resource not found.");
        }

        public static Song FromOggFile(string filePath)
        {
            var fstream = File.OpenRead(filePath);
            return FromOggStream(fstream);
        }
    }

    public interface IMusicReader
    {
        byte[] ReadSamples(int samples);
    }
    
    public sealed class VorbisStream : IMusicReader
    {
        private const double sc16 = 0x7FFF + 0.4999999999999999;

        private float[] _buffer;
        
        private VorbisReader _reader;

        public VorbisStream(VorbisReader reader)
        {
            _reader = reader;
            _buffer = new float[_reader.Channels * 1024];
        }

        public byte[] ReadSamples(int samples)
        {
            var bytes = Array.Empty<byte>();
            var offset = 0;
            while (samples > 0)
            {
                var sampleCount = Math.Min(samples, _buffer.Length);

                var read = _reader.ReadSamples(_buffer, 0, sampleCount);

                var byteCount = read * sizeof(ushort);
                
                if (offset + byteCount > bytes.Length)
                    Array.Resize(ref bytes, offset + byteCount);

                for (var i = 0; i < read; i++)
                {
                    var sample = _buffer[i];
                    var asPCM = (ushort) (sample * sc16);

                    bytes[offset] = (byte) asPCM;
                    bytes[offset + 1] = (byte) (asPCM >> 8);

                    offset += sizeof(ushort);
                }

                samples -= sampleCount;
            }            

            return bytes;
        }
    }
}