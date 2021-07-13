using System;
using System.Collections.Generic;

namespace Thundershock.Core.Rendering
{
    public interface IEffectProgramList : IEnumerable<Effect.EffectProgram>
    {
        Effect.EffectProgram this[string name] { get; }
    }
    
    public interface IEffect
    {
        IEffectProgramList Programs { get; }
    }
    
    public sealed class BasicEffect : IEffect
    {
        private Effect _effect;

        public BasicEffect(GraphicsProcessor gpu)
        {
            // Retrieve the built-in effect shader program.
            var text = string.Empty;
            if (!Resource.TryGetString(GetType().Assembly, "Thundershock.Core.Resources.Effects.BasicEffect.glsl",
                out text))
            {
                throw new InvalidOperationException(
                    "Cannot create the BasicEffect effect, the embedded GLSL shader resource is missing. This should never happen. Rebuild/reinstall Thundershock or your game now.");
            }
            
            // Compile the shader.
            _effect = ShaderPipeline.CompileShader(gpu, text);
        }


        public IEffectProgramList Programs => _effect.Programs;
    }
}