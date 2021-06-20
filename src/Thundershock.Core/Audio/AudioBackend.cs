using System;

namespace Thundershock.Core.Audio
{
    public abstract class AudioBackend : IDisposable
    {
        private bool _disposed = false;
        
        public abstract float MasterVolume { get; set; }

        public abstract AudioOutput OpenAudioOutput();
        
        public void Dispose()
        {
            Dispose(!_disposed);
            _disposed = true;
        }

        protected abstract void Dispose(bool disposing);
    }
}