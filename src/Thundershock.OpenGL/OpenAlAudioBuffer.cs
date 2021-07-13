using System;
using System.Linq;
using Silk.NET.OpenAL;
using Thundershock.Core.Audio;

namespace Thundershock.OpenGL
{
    internal sealed class OpenAlAudioBuffer : IAudioBuffer
    {
        private const double Sc16 = 0x7FFF + 0.4999999999999999;

        private double _power = double.NegativeInfinity;
        private AL _al;
        private uint _bufferId;
        private int _channelCount;
        private int _sampleRate;
        private byte[] _buffer = Array.Empty<byte>();

        public OpenAlAudioBuffer(AL al, int channels, int sampleRate)
        {
            _al = al;
            _channelCount = channels;
            _sampleRate = sampleRate;

            _bufferId = _al.GenBuffer();
        }
        
        public void Dispose()
        {
            _al.DeleteBuffer(_bufferId);
        }

        public uint Id => _bufferId;

        public int Channels => _channelCount;
        public int SampleRate => _sampleRate;

        public double Power => _power;

        public ReadOnlySpan<byte> Buffer => _buffer;
        
        public void SetBuffer(ReadOnlySpan<byte> buffer)
        {
            // Copy the data over.
            _buffer = buffer.ToArray();
            
            // Audio format
            var format = Channels switch
            {
                1 => BufferFormat.Mono16,
                2 => BufferFormat.Stereo16,
                _ => throw new InvalidOperationException("Unsupported channel count.")
            };
            
            // Buffer the data into OpenAL
            _al.BufferData(_bufferId, format, _buffer, SampleRate);
            
            // Let's do some FFT!
            unsafe
            {
                var power = 0d;
                Span<double> doubleFault = stackalloc double[buffer.Length / sizeof(ushort)];
                var j = 0;
                fixed (byte* ptr = buffer)
                {
                    for (var i = 0; i < buffer.Length; i += sizeof(ushort))
                    {
                        var sample = (ushort) *(ptr + i + 1);
                        sample += (ushort) ((*(ptr + i)) << 8);
                        doubleFault[j] = (sample / Sc16);
                        j++;
                    }
                }

                try
                {
                    var arr = doubleFault.ToArray();
                    var hanning = FftSharp.Window.Hanning(doubleFault.Length);
                    FftSharp.Window.ApplyInPlace(hanning, arr);

                    power = FftSharp.Transform.FFTpower(arr).Average();
                }
                catch
                {
                    power = double.NegativeInfinity;
                }

                _power = power;
            }

        }
    }
}