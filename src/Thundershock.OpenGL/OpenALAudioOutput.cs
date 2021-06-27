using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using SDL2;
using Silk.NET.OpenAL;
using Thundershock.Core.Audio;
using Thundershock.Core.Debugging;

namespace Thundershock.OpenGL
{
    public sealed class OpenAlAudioOutput : AudioOutput
    {
        private ConcurrentQueue<byte[]> _queue = new();
        private bool _teardown;
        private ManualResetEvent _teardownWait = new(false);
        private AL _al;
        private uint _source;
        private int _pendingBufferCount;
        private object _lockObject = new();
        private int _sampleRate;
        private int _channels;

        public override int PendingBufferCount
            => _queue.Count;

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
            
            _al.SetSourceProperty(_source, SourceBoolean.Looping, true);
            ThrowOnError();

            new Thread(bufferThread).Start();
        }

        protected override void Dispose(bool disposing)
        {
            _teardown = true;
            _teardownWait.WaitOne();
        }

        public override void Play()
        {
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
            while (!_teardown)
            {
                _al.GetSourceProperty(_source, GetSourceInteger.SourceState, out int state);
                _al.GetSourceProperty(_source, GetSourceInteger.BuffersQueued, out int queued);
                _al.GetSourceProperty(_source, GetSourceInteger.BuffersProcessed, out int processed);

                while (processed > 0)
                {
                    unsafe
                    {
                        var buf = stackalloc uint[1];
                        _al.SourceUnqueueBuffers(_source, 1, buf);
                        processed--;
                    }
                }
                
                if (queued > 0 && state == (int) SourceState.Stopped)
                {
                    _al.SourcePlay(_source);
                }
            }

            _teardownWait.Set();
        }
    }
}