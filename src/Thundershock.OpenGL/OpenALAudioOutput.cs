using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Silk.NET.OpenAL;
using Thundershock.Core.Audio;
using Thundershock.Core.Debugging;

namespace Thundershock.OpenGL
{
    public sealed class OpenAlAudioOutput : AudioOutput
    {
        private bool _teardown;
        private ManualResetEvent _teardownWait = new(false);
        private ConcurrentQueue<IAudioBuffer> _bufferQueue = new();
        private IAudioBuffer _currentBuffer;
        private AL _al;
        private uint _source;
        
        public override AudioState State
        {
            get
            {
                _al.GetSourceProperty(_source, GetSourceInteger.SourceState, out var state);

                return (SourceState) state switch
                {
                    SourceState.Playing => AudioState.Playing,
                    SourceState.Stopped => AudioState.Stopped,
                    SourceState.Paused => AudioState.Paused,
                    _ => AudioState.Stopped
                };
            }
        }

        public override double Power
            => _currentBuffer?.Power ?? double.NegativeInfinity;

        public override int PendingBufferCount
            => _bufferQueue.Count;
        
        public override float Volume
        {
            get
            {
                _al.GetSourceProperty(_source, SourceFloat.Gain, out var vol);
                return vol;
            }
            set
            {
                _al.SetSourceProperty(_source, SourceFloat.Gain, value);
            }
        }
        
        public OpenAlAudioOutput(AL al)
        {
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

            new Thread(BufferThread).Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // I. Fucking. Want. You. To. Stop.
                Stop();
                
                _teardown = true;
                
                _teardownWait.WaitOne();
                
                // delete the source
                _al.DeleteSource(_source);
            }
        }

        public override void Stop()
        {
            _al.SourceStop(_source);
        }
        
        public override void Play()
        {
            _al.SourcePlay(_source);
            ThrowOnError();
        }

        public override void SubmitBuffer(IAudioBuffer buffer)
        {
            if (_currentBuffer != null)
            {
                _bufferQueue.Enqueue(buffer);
            }
            else
            {
                _currentBuffer = buffer;
            }
            
            _al.SourceQueueBuffers(_source, new[] {buffer.Id});
        }

        private void ThrowOnError()
        {
            var error = _al.GetError();
            if (error != AudioError.NoError)
                throw new InvalidOperationException("OpenAL threw an error: " + error);
        }

        private void BufferThread()
        {
            unsafe
            {
                var buf = stackalloc uint[1];

                while (!_teardown)
                {
                    _al.GetSourceProperty(_source, GetSourceInteger.SourceState, out _);
                    _al.GetSourceProperty(_source, GetSourceInteger.BuffersProcessed, out var processed);

                    if (processed > 0)
                    {
                        _al.SourceUnqueueBuffers(_source, 1, buf);

                        Debug.Assert(_currentBuffer.Id == buf[0],
                            "What the hell? The queued buffer IDs do not match.");

                        FireBufferProcessed(_currentBuffer);

                        if (_bufferQueue.TryDequeue(out var dequeued))
                        {
                            _currentBuffer = dequeued;
                        }
                        else
                        {
                            _currentBuffer = null;
                        }
                        
                        processed--;
                    }

                    _al.GetSourceProperty(_source, GetSourceInteger.BuffersQueued, out _);
                    
                    Thread.Sleep(10); // Let's... Not tie up this entire CPU core...
                }
            }

            _teardownWait.Set();
        }
    }
}