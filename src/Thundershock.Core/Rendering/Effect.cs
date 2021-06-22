using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Thundershock.Core.Rendering
{
    public sealed class Effect : IEffect
    {
        private sealed class EffectProgramList : IEffectProgramList
        {
            private Effect _owner;

            public EffectProgramList(Effect owner)
            {
                _owner = owner;
            }
            
            public EffectProgram this[string name]
            {
                get => _owner._programs.First(x => x.Name == name);
            }

            public IEnumerator<EffectProgram> GetEnumerator()
            {
                return _owner._programs.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _owner._programs.GetEnumerator();
            }
        }

        public sealed class EffectProgram
        {
            private EffectParameterList _params;
            private Effect _owner;
            private uint _id;
            private string _name;

            public uint Id => _id;
            
            public string Name => _name;

            public EffectParameterList Parameters => _params;
            
            internal EffectProgram(Effect owner, string name, uint compiledShader)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _name = name ?? throw new ArgumentNullException(nameof(name));
                _id = compiledShader;
                _params = new EffectParameterList(_owner._gpu, this);
            }

            public void Apply()
            {
                _owner._gpu.SetActiveShaderProgram(_id);
            }
            
            
        }

        private IEffectProgramList _programList;
        private GraphicsProcessor _gpu;
        private List<EffectProgram> _programs = new List<EffectProgram>();

        public IEffectProgramList Programs => _programList;
        
        public Effect(GraphicsProcessor gpu)
        {
            _gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
            _programList = new EffectProgramList(this);
        }

        public void AddProgram(ShaderProgram program)
        {
            if (_programs.Any(x => x.Name == program.Name))
                throw new InvalidOperationException("Program name already added to ffect.");

            var id = program.Compile(_gpu);
            
            _programs.Add(new EffectProgram(this, program.Name, id));
        }
    }

    public sealed class EffectParameterList
    {
        private Effect.EffectProgram _program;
        private GraphicsProcessor _gpu;

        private Dictionary<string, EffectParameter> _cache = new Dictionary<string, EffectParameter>();
        
        public EffectParameterList(GraphicsProcessor gpu, Effect.EffectProgram program)
        {
            _gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
            _program = program ?? throw new ArgumentNullException(nameof(program));
        }
        
        public EffectParameter this[string name]
        {
            get => GetParamInternal(name);
        }

        private EffectParameter GetParamInternal(string name)
        {
            if (_cache.ContainsKey(name))
            {
                // No need to hit the GPU for the effect parameter location.
                return _cache[name];
            }
            
            // Ask the GPU where the parameter is.
            var param = _gpu.GetEffectParameter(_program, name);
            
            // Cache it.
            _cache.Add(name, param);

            return param;
        }
    }
}