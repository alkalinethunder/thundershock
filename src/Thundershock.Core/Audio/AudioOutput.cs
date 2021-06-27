using System;

namespace Thundershock.Core.Audio
{
    public abstract class AudioOutput : IDisposable
    {
        private bool _disposed = false;
        
        public abstract int PendingBufferCount { get; }
        
        public void Dispose()
        {
            Dispose(!_disposed);
        }

        public abstract float Volume { get; set; }
        
        protected abstract void Dispose(bool disposing);
        public abstract void Play();

        public abstract void SubmitBuffer(byte[] buffer);
    }
}