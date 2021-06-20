using System;
using System.Reflection;
using SDL2;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenAL;
using Thundershock.Core.Audio;

namespace Thundershock.OpenGL
{
    public sealed unsafe class SDLAudioBackend : AudioBackend
    {
        private ALContext _alc;
        private Device* _device;
        private AL _al;
        
        public SDLAudioBackend()
        {
            var alctx = ALContext.CreateDefaultContext("soft_oal.dll");
            
            _alc = (ALContext)typeof(ALContext)
                .GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, 
                    null, 
                    new[] { typeof(INativeContext) }, 
                    null
                )
                .Invoke(new object[] { alctx });
            
            _device = _alc.OpenDevice(string.Empty);

            var ctx = _alc.CreateContext(_device, null);
            _alc.MakeContextCurrent(ctx);

            _al = AL.GetApi(true);
        }

        public override float MasterVolume { get; set; }

        public override AudioOutput OpenAudioOutput()
        {
            return new OpenALAudioOutput(_al);
        }
        
        protected override void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }
    }
}