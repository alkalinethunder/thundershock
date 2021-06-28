using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SDL2;
using Silk.NET.OpenAL;
using Thundershock.Core.Audio;
using Thundershock.Core.Debugging;

namespace Thundershock.OpenGL
{
    public sealed class OpenAlAudioOutput : AudioOutput
    {
        private const double sc16 = 0x7FFF + 0.4999999999999999;

        private double _floor = 60;
        private double _sensitivity = 2;
        private double _power;
        private ConcurrentQueue<double> _powerQueue = new ConcurrentQueue<double>();
        private AudioState _state = AudioState.Stopped;
        private ConcurrentQueue<byte[]> _queue = new();
        private bool _teardown;
        private ManualResetEvent _teardownWait = new(false);
        private AL _al;
        private uint _source;
        private int _pendingBufferCount;
        private object _lockObject = new();
        private int _sampleRate;
        private int _channels;

        public override AudioState State => _state;

        public override double Power
            => (_power + _floor) * _sensitivity;
        
        public override int PendingBufferCount
            => _pendingBufferCount;
        
        public override float Volume
        {
            get
            {
                var vol = 0f;
                _al.GetSourceProperty(_source, SourceFloat.Gain, out vol);
                return vol;
            }
            set
            {
                _al.SetSourceProperty(_source, SourceFloat.Gain, value);
            }
        }

        public BufferFormat BufferFormat
        {
            get
            {
                return _channels switch
                {
                    1 => BufferFormat.Mono16,
                    2 => BufferFormat.Stereo16,
                    _ => throw new InvalidOperationException("WHAT THE ACTUAL FUCK")
                };
            }
        }

        public OpenAlAudioOutput(AL al, int sampleRate, int channels)
        {
            _sampleRate = sampleRate;
            _channels = channels;
            _al = al;
            _source = _al.GenSource();
            ThrowOnError();
            
            Logger.GetLogger().Log("Generating OpenAL source: " + _source.ToString());
            ThrowOnError();
            
            _al.SetSourceProperty(_source, SourceFloat.Gain, 1);
            ThrowOnError();
            
            _al.SetSourceProperty(_source, SourceFloat.Pitch, 1);
            ThrowOnError();
            
            _al.SetSourceProperty(_source, SourceBoolean.Looping, false);
            ThrowOnError();

            new Thread(bufferThread).Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // I. Fucking. Want. You. To. Stop.
                Stop();
                
                _teardown = true;
                
                _teardownWait.WaitOne();

                // delete any queued up audio buffers.
                unsafe
                {
                    var buf = stackalloc uint[1];

                    _al.GetSourceProperty(_source, GetSourceInteger.BuffersQueued, out var queued);

                    while (queued > 0)
                    {
                        _al.SourceUnqueueBuffers(_source, 1, buf);
                        _al.DeleteBuffer(buf[0]);

                        queued--;
                    }
                }
                
                // delete the source
                _al.DeleteSource(_source);
            }
        }

        public override void Stop()
        {
            _state = AudioState.Stopped;
            _al.SourceStop(_source);
        }
        
        public override void Play()
        {
            _state = AudioState.Playing;
            _al.SourcePlay(_source);
            ThrowOnError();
        }

        public override void SubmitBuffer(byte[] buffer)
        {
            var alBuffer = _al.GenBuffer();
            ThrowOnError();

            _al.BufferData(alBuffer, BufferFormat, buffer, _sampleRate);
            ThrowOnError();
            
            _al.SourceQueueBuffers(_source, new uint[] {alBuffer});
            ThrowOnError();
            
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
                        doubleFault[j] = (double) (sample / sc16);
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

                if (_pendingBufferCount > 0)
                {
                    _powerQueue.Enqueue(power);
                }
                else
                {
                    _power = power;
                }
            }
            
            _pendingBufferCount++;
        }

        private void ThrowOnError()
        {
            var error = _al.GetError();
            if (error != AudioError.NoError)
                throw new InvalidOperationException("OpenAL threw an error: " + error);
        }

        private void bufferThread()
        {
            unsafe
            {
                var buf = stackalloc uint[1];

                while (!_teardown)
                {
                    _al.GetSourceProperty(_source, GetSourceInteger.SourceState, out int state);
                    _al.GetSourceProperty(_source, GetSourceInteger.BuffersQueued, out int queued);
                    _al.GetSourceProperty(_source, GetSourceInteger.BuffersProcessed, out int processed);

                    while (processed > 1)
                    {
                        _al.SourceUnqueueBuffers(_source, 1, buf);

                        _al.DeleteBuffer(buf[0]);

                        processed--;
                        _powerQueue.TryDequeue(out _power);
                        _pendingBufferCount--;
                    }

                    if (queued > 0 && state == (int) SourceState.Stopped && _state == AudioState.Playing)
                    {
                        _al.SourcePlay(_source);
                    }
                }
            }

            _teardownWait.Set();
        }
    }
}