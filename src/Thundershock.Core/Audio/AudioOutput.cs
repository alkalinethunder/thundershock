using System;

namespace Thundershock.Core.Audio
{
    public abstract class AudioOutput : IDisposable
    {
        private bool _disposed = false;
        
        public abstract int PendingBufferCount { get; }
        public abstract double Power { get; }
        public abstract AudioState State { get; }

        public event EventHandler<IAudioBuffer> BufferProcessed;
        
        public void Dispose()
        {
            Dispose(!_disposed);
        }

        public abstract void Stop();
        public abstract float Volume { get; set; }
        
        protected abstract void Dispose(bool disposing);
        public abstract void Play();

        public abstract void SubmitBuffer(IAudioBuffer buffer);

        protected void FireBufferProcessed(IAudioBuffer buffer)
        {
            EntryPoint.CurrentApp.EnqueueAction(() => { BufferProcessed?.Invoke(this, buffer); });
        }
    }

    public enum AudioState
    {
        Stopped,
        Playing,
        Paused
    }
}