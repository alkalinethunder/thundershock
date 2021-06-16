using System;
using Thundershock.Core.Debugging;
using Thundershock.Debugging;
using static OpenGL.GL;

namespace Thundershock.OpenGL
{
    public sealed class GlShaderCompiler
    {
        private GlGraphicsProcessor _gpu;
        
        public GlShaderCompiler(GlGraphicsProcessor gpu)
        {
            _gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
        }

        private uint CompileShader(string shader, int type)
        {
            var shaderObject = glCreateShader(type);
            glShaderSource(shaderObject, shader);
            glCompileShader(shaderObject);
            
            int result = 0;
            unsafe
            {
                glGetShaderiv(shaderObject, GL_COMPILE_STATUS, &result);
            }

            if (result == GL_FALSE)
            {
                int length = 0;
                var errorText = glGetShaderInfoLog(shaderObject);

                var typeName = type switch
                {
                    GL_VERTEX_SHADER => "Vertex shader",
                    GL_FRAGMENT_SHADER => "Fragment shader",
                    _ => "Shader"
                };

                Logger.GetLogger().Log($"{typeName} compilation failed.", LogLevel.Error);
                Logger.GetLogger().Log(errorText, LogLevel.Error);
                
                glDeleteShader(shaderObject);

                return 0;
            }
            
            return shaderObject;
        }
        
        public uint CompileShaderProgram(string vertexShader, string fragmentShader)
        {
            var program = glCreateProgram();

            var vs = CompileShader(vertexShader, GL_VERTEX_SHADER);
            var ps = CompileShader(fragmentShader, GL_FRAGMENT_SHADER);

            if (ps == 0 || vs == 0)
            {
                glDeleteProgram(program);
                return 0;
            }
            
            glAttachShader(program, vs);
            glAttachShader(program, ps);
            
            glLinkProgram(program);

            glValidateProgram(program);

            glDeleteShader(vs);
            glDeleteShader(ps);
            
            return program;
        }
    }
}