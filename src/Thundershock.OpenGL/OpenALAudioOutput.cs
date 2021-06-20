using System;
using SDL2;
using Silk.NET.OpenAL;
using Thundershock.Core.Audio;

namespace Thundershock.OpenGL
{
    public sealed class OpenALAudioOutput : AudioOutput
    {
        private AL _al;
        private uint _source;
        private uint _buffer;

        public override int PendingBufferCount
        {
            get
            {
                return 0;
            }
        }

        public OpenALAudioOutput(AL al)
        {
            _al = al;
            _source = _al.GenSource();

            _al.SetSourceProperty(_source, SourceFloat.Gain, 1);
            
            _al.SetSourceProperty(_source, SourceBoolean.Looping, true);
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }

        public override void Play()
        {
            _al.SourcePlay(_source);
        }

        public override void SubmitBuffer(byte[] buffer)
        {
            var alBuffer = _al.GenBuffer();
            _al.BufferData(alBuffer, BufferFormat.Stereo8, buffer, 22050);
            _al.SourceQueueBuffers(_source, new uint[] {alBuffer});
        }
    }
}