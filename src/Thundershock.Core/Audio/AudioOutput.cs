using System;

namespace Thundershock.Core.Audio
{
    public abstract class AudioOutput : IDisposable
    {
        private bool _disposed = false;
        
        public abstract int PendingBufferCount { get; }
        public abstract double Power { get; }
        public abstract AudioState State { get; }
        
        public void Dispose()
        {
            Dispose(!_disposed);
        }

        public abstract void Stop();
        public abstract float Volume { get; set; }
        
        protected abstract void Dispose(bool disposing);
        public abstract void Play();

        public abstract void SubmitBuffer(byte[] buffer);
    }

    public enum AudioState
    {
        Stopped,
        Playing,
        Paused
    }
}