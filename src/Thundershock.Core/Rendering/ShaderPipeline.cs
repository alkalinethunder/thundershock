using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thundershock.Core.Debugging;

namespace Thundershock.Core.Rendering
{
    public static class ShaderPipeline
    {
        private struct PreprocessorMacro
        {
            public int Line;
            public int Index;
            public string Content;
        }
        
        private static List<ShaderProgram> TryPreprocess(string sourceCode)
        {
            // split into lines.
            var lines = sourceCode.Split(Environment.NewLine);
            
            // Find all Thundershock preprocessor macros.
            var macros = new List<PreprocessorMacro>();
            var i = 0;
            foreach (var line in lines)
            {
                var lookFor = "#pragma ts ";
                var pragmaTs = line.IndexOf(lookFor, StringComparison.Ordinal);

                if (pragmaTs >= 0)
                {
                    var content = line.Substring(pragmaTs + lookFor.Length);

                    var data = new PreprocessorMacro
                    {
                        Line = i,
                        Index = pragmaTs,
                        Content = content
                    };

                    macros.Add(data);
                }

                i++;
            }

            // Now we can go through the macros.
            var inProgram = false;
            var inShader = false;
            var program = null as ShaderProgram;
            var shader = null as ShaderDefinition;
            var shaderStart = -1;
            var programs = new List<ShaderProgram>();
            foreach (var macro in macros)
            {
                if (string.IsNullOrWhiteSpace(macro.Content))
                {
                    Logger.Log(
                            $"Shader preprocessor: Warning PP1 (ln {macro.Line} ch {macro.Index}): Empty macro. Ignoring.",
                            LogLevel.Warning);
                    continue;
                }
                
                // Tokenize the preprocessor command.
                var command = macro.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var name = command.First();
                var args = command.Skip(1).ToArray();

                if (name == "program")
                {
                    if (inProgram)
                    {
                        throw new ShaderPreprocessorException(macro.Line, macro.Index,
                            "Cannot define a new program inside the scope of another program. Did you forget a #pragma ts end?");
                    }

                    if (!args.Any())
                        throw new ShaderPreprocessorException(macro.Line, macro.Index, "Missing program name!");

                    program = new ShaderProgram(args.First());
                    inProgram = true;
                    continue;
                }

                if (name == "shader")
                {
                    if (inShader)
                        throw new ShaderPreprocessorException(macro.Line, macro.Index,
                            "Cannot define a shader inside the scope of another shader. Did you forget a #pragma ts end?");
                    
                    if (!inProgram)
                        throw new ShaderPreprocessorException(macro.Line, macro.Index,
                            "Shaders must be defined inside the scope of a Program.");

                    if (!args.Any())
                        throw new ShaderPreprocessorException(macro.Line, macro.Index, "Missing shader name!");

                    shader = new ShaderDefinition(args.First());
                    shaderStart = macro.Line + 1;
                    inShader = true;
                    continue;
                }

                if (name == "compile")
                {
                    if (inShader)
                        throw new ShaderPreprocessorException(macro.Line, macro.Index,
                            "Cannot define a shader compilation inside the scope of a shader. Did you forget a #pragma ts end?");

                    if (!inProgram)
                        throw new ShaderPreprocessorException(macro.Line, macro.Index,
                            "Cannot define a shader compilation outside the scope of a Program.");

                    if (args.Length < 2)
                        throw new ShaderPreprocessorException(macro.Line, macro.Index,
                            "Missing como;e type and shader name!");

                    var sType = args[0];
                    var sName = args[1];

                    var type = sType switch
                    {
                        "vert" => ShaderCompilation.VertexShader,
                        "frag" => ShaderCompilation.FragmentShader,
                        _ => throw new ShaderPreprocessorException(macro.Line, macro.Index,
                            "Unsupported shader type " + sType + ".")
                    };

                    if (!program.HasShader(sName))
                        throw new ShaderPreprocessorException(macro.Line, macro.Index,
                            "Shader " + sName + " not defined in the program!");

                    if (program.WillCompile(sName))
                        throw new ShaderPreprocessorException(macro.Line, macro.Index,
                            "Shader compilation has already been defined.");
                    
                    program.SetCompileType(sName, type);
                    continue;
                }
                
                if (name == "end")
                {
                    if (inShader)
                    {
                        var srcBuilder = new StringBuilder();
                        for (var l = shaderStart; l < macro.Line; l++)
                        {
                            var sLine = lines[l];
                            if (l == macro.Line)
                                sLine = sLine.Substring(0, macro.Index);
                            srcBuilder.AppendLine(sLine);
                        }

                        shader.SourceCode = srcBuilder.ToString();
                        program.AddShader(shader);
                        shader = null;
                        shaderStart = -1;
                        inShader = false;
                        continue;
                    }

                    if (inProgram)
                    {
                        if (!program.HasCompilations)
                        {
                            Logger.Log("Empty shader program detected!", LogLevel.Warning);
                            continue;
                        }

                        programs.Add(program);
                        inProgram = false;
                        program = null;
                    }
                }
            }

            return programs;
        }
        
        public static Effect CompileShader(GraphicsProcessor gpu, string sourceCode)
        {
            var programs = TryPreprocess(sourceCode);

            var effect = new Effect(gpu);

            foreach (var program in programs)
                effect.AddProgram(program);

            return effect;
        }
    }

    public class ShaderPreprocessorException : Exception
    {
        public ShaderPreprocessorException(int line, int index, string message)
            : base($"ERROR (ln: {line} ch: {index}): {message}")
        {
            
        }
    }
    
    public class ShaderDefinition
    {
        public string Name { get; }
        public string SourceCode { get; set; }
            
        public ShaderDefinition(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }

    public enum ShaderCompilation
    {
        VertexShader,
        FragmentShader
    }
        
    public class ShaderProgram
    {
        private List<ShaderDefinition> _shaders = new List<ShaderDefinition>();
        private Dictionary<string, ShaderCompilation> _compilations = new Dictionary<string, ShaderCompilation>();
            
        public string Name { get; private set; }

        public bool HasCompilations => _compilations.Any();
            
        public ShaderProgram(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public bool HasShader(string name)
            => _shaders.Any(x => x.Name == name);

        public bool WillCompile(string name)
            => _compilations.ContainsKey(name);
            
        public void SetCompileType(string shaderName, ShaderCompilation type)
        {
            _compilations.Add(shaderName, type);
        }

        public void AddShader(ShaderDefinition shader)
        {
            if (!_shaders.Contains(shader))
            {
                _shaders.Add(shader);
            }
        }

        public uint Compile(GraphicsProcessor gpu)
        {
            var programId = gpu.CreateShaderProgram();

            foreach (var compilation in _compilations)
            {
                var shader = _shaders.First(x => x.Name == compilation.Key);
                gpu.CompileGlsl(programId, compilation.Value, shader.SourceCode);
            }

            gpu.VerifyShaderProgram(programId);

            return programId;
        }
    }
}