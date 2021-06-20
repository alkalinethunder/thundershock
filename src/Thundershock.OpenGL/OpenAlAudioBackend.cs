using System;
using System.Reflection;
using SDL2;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenAL;
using Thundershock.Core.Audio;

namespace Thundershock.OpenGL
{
    public sealed class OpenAlAudioBackend : AudioBackend
    {
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
                var ctx = _alc.CreateContext(device, null);
                _alc.MakeContextCurrent(ctx);
            }
        }

        public override float MasterVolume { get; set; }

        public override AudioOutput OpenAudioOutput()
        {
            return new OpenAlAudioOutput(_al);
        }
        
        protected override void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }
    }
}