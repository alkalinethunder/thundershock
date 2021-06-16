using System;
using Thundershock.Core;
using Thundershock.Core.Rendering;

using static OpenGL.GL;

namespace Thundershock.OpenGL
{
    public sealed class GlGraphicsProcessor : GraphicsProcessor
    {
        private GlShaderCompiler _shaderCompiler;
        private uint _vertexBuffer;
        private uint _indexBuffer;
        private uint _basicEffect;
        
        internal GlGraphicsProcessor()
        {
            // Create the shader compiler object
            _shaderCompiler = new GlShaderCompiler(this);
            
            // generate the vertex buffer and index buffer objects.
            _vertexBuffer = glGenBuffer();
            _indexBuffer = glGenBuffer();
            
            // bind the vbo and ibo to the GPU.
            glBindBuffer(GL_ARRAY_BUFFER, _vertexBuffer);
            glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _indexBuffer);
            
            // Compile the basic effect shaders.
            CompileBasicEffect();
        }


        public override void Clear(Color color)
        {
            var vec4 = color.ToVector4();
            glClearColor(vec4.X, vec4.Y, vec4.Z, vec4.W);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
            
            // Test code please ignore
            var vertices = new float[]
            {
                -0.5f, -0.5f,
                0.0f, 0.5f,
                0.5f, -0.5f
            };

            unsafe
            {
                fixed (void* buf = vertices)
                {
                    glBufferData(GL_ARRAY_BUFFER, 6 * sizeof(float), buf, GL_STATIC_DRAW);
                }
            }

            glEnableVertexAttribArray(0);
            glVertexAttribPointer(0, 2, GL_FLOAT, false, 2 * sizeof(float), IntPtr.Zero);

            glUseProgram(_basicEffect);
            
            glDrawArrays(GL_TRIANGLES, 0, 3);
        }

        public override void DrawIndexedPrimitives(PrimitiveType type, Vertex[] vertices, int[] indices, int indexStart, int primitiveCount)
        {
            
        }

        private void CompileBasicEffect()
        {
            // Resource names.
            var vert = "Thundershock.OpenGL.Resources.Shaders.BasicShader.vert";
            var frag = "Thundershock.OpenGL.Resources.Shaders.BasicShader.frag";

            // Our DLL file.
            var asm = this.GetType().Assembly;

            if (Resource.TryGetString(asm, vert, out var vertShader) &&
                Resource.TryGetString(asm, frag, out var fragShader))
            {
                _basicEffect = _shaderCompiler.CompileShaderProgram(vertShader, fragShader);
            }
            else
            {
                _basicEffect = 0;
            }

            if (_basicEffect == 0)
            {
                throw new InvalidOperationException(
                    "The basic shaders failed to compile. The OpenGL graphics pipeline cannot continue to run without them. Please see the debug log for shader compile errors.");
            }
        }
    }
}