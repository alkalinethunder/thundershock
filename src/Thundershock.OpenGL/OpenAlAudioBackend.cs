using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using SDL2;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenAL;
using Thundershock.Core;
using Thundershock.Core.Audio;
using Thundershock.Core.Debugging;
using Thundershock.Debugging;

namespace Thundershock.OpenGL
{
    public sealed class OpenAlAudioBackend : AudioBackend
    {
        private List<AudioOutput> _outputs = new List<AudioOutput>();
        private List<IAudioBuffer> _buffers = new List<IAudioBuffer>();
        private AL _al;
        private ALContext _alc;
        
        public OpenAlAudioBackend()
        {
            _al = AL.GetApi(true);
            
            // So.
            // Current version of Silk.NET is smashed and doesn't let us use soft_oal
            // for the ALContext (but does for the OpenAL API itself).
            //
            // This'll be fixed in two weeks.
            //
            // Oh and they protected the native constructor. Ew. That'll also
            // be fixed in two weeks.
            //
            // For now... YOUR IDE WILL SCREAM AT YOU IN TERROR BECAUSE I'M USING
            // SUPER SPOOKY SCARY EVIL REFLECTION STUFF but...
            _alc = (ALContext) typeof(ALContext)
                .GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                    null,
                    new[]
                    {
                        typeof(INativeContext)
                    },
                    null
                )?.Invoke(new[] {_al.Context});

            unsafe
            {
                var device = _alc.OpenDevice(string.Empty);
                var error = _alc.GetError(device);
                if (error != ContextError.NoError)
                    throw new InvalidOperationException("OpenAL threw an error: " + error);

                
                var ctx = _alc.CreateContext(device, null);
                error = _alc.GetError(device);
                if (error != ContextError.NoError)
                    throw new InvalidOperationException("OpenAL threw an error: " + error);

                
                _alc.MakeContextCurrent(ctx);
                error = _alc.GetError(device);
                if (error != ContextError.NoError)
                    throw new InvalidOperationException("OpenAL threw an error: " + error);

            }
            
            ThrowOnError();
        }

        public override float MasterVolume { get; set; }

        public override IAudioBuffer CreateBuffer(int channels, int sampleRate)
        {
            var buffer = new OpenAlAudioBuffer(_al, channels, sampleRate);
            _buffers.Add(buffer);
            return buffer;
        }

        public override AudioOutput OpenAudioOutput()
        {
            var aud = new OpenAlAudioOutput(_al);
            _outputs.Add(aud);
            return aud;
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Sometimes love don't feel like it should....
                while (_outputs.Any())
                {
                    var output = _outputs.First();
                    
                    // BUT IT HURTS SO GOOD.
                    output.Dispose();
                    _outputs.Remove(output);
                }
                
                // Sometimes love don't feel like it should....
                while (_buffers.Any())
                {
                    var buffer = _buffers.First();
                    
                    // BUT IT HURTS SO GOOD.
                    buffer.Dispose();
                    _buffers.Remove(buffer);
                }
            }
        }
        
        private void ThrowOnError()
        {
            var error = _al.GetError();
            if (error != AudioError.NoError)
                throw new InvalidOperationException("OpenAL threw an error: " + error);
        }


   }
}