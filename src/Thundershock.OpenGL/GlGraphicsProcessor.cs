using System;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Rendering;

using static OpenGL.GL;

namespace Thundershock.OpenGL
{
    public sealed class GlGraphicsProcessor : GraphicsProcessor
    {
        private float[] _vertexData = Array.Empty<float>();
        private GlShaderCompiler _shaderCompiler;
        private uint _vertexBuffer;
        private uint _indexBuffer;
        private uint _vao;
        private uint _basicEffect;
        
        internal GlGraphicsProcessor()
        {
            // Create the shader compiler object
            _shaderCompiler = new GlShaderCompiler(this);
            
            // Vertex array object.
            _vao = glGenVertexArray();
            glBindVertexArray(_vao);

            // generate the vertex buffer and index buffer objects.
            _vertexBuffer = glGenBuffer();
            _indexBuffer = glGenBuffer();
            
            // bind the vbo and ibo to the GPU.
            glBindBuffer(GL_ARRAY_BUFFER, _vertexBuffer);
            glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _indexBuffer);
            
            // Compile the basic effect shaders.
            CompileBasicEffect();
            
            // unbind the vertex buffer
            glBindBuffer(GL_ARRAY_BUFFER, 0);
        }


        public override void Clear(Color color)
        {
            var vec4 = color.ToVector4();
            glClearColor(vec4.X, vec4.Y, vec4.Z, vec4.W);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        }

        public override void DrawPrimitives(PrimitiveType primitiveType, ReadOnlySpan<int> indices, int primitiveCount)
        {
            glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _indexBuffer);

            unsafe
            {
                fixed (void* data = indices)
                {
                    glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(int) * indices.Length, data, GL_DYNAMIC_DRAW);
                }
            }
            
            var type = primitiveType switch
            {
                PrimitiveType.LineStrip => GL_LINE_STRIP,
                PrimitiveType.TriangleStrip => GL_TRIANGLE_STRIP,
                _ => throw new NotSupportedException()
            };

            glUseProgram(_basicEffect);
            
            unsafe
            {
                glDrawElements(type, indices.Length, GL_UNSIGNED_INT, null);
            }
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

        public override uint CreateVertexBuffer()
        {
            var vbo = glGenBuffer();
            return vbo;
        }

        public override void DeleteVertexBuffer(uint vbo)
        {
            glDeleteBuffer(vbo);
        }

        public override void SubmitVertices(uint vbo, ReadOnlySpan<Vertex> vertices)
        {
            glBindBuffer(GL_ARRAY_BUFFER, vbo);
            
            var posSize = 3;
            var colorSize = 4;
            var texCoordSize = 2;

            var vertSize = posSize + colorSize + texCoordSize;
            var arrSize = vertSize * vertices.Length;

            if (_vertexData.Length != arrSize)
                Array.Resize(ref _vertexData, arrSize);

            unsafe
            {
                fixed (Vertex* vertex = vertices)
                {
                    var current = vertex;

                    for (var i = 0; i < arrSize; i += vertSize)
                    {
                        _vertexData[i] = current->Position.X;
                        _vertexData[i + 1] = current->Position.Y;
                        _vertexData[i + 2] = current->Position.Z;
                        
                        _vertexData[i + 3] = current->Color.X;
                        _vertexData[i + 4] = current->Color.Y;
                        _vertexData[i + 5] = current->Color.Z;
                        _vertexData[i + 6] = current->Color.W;

                        _vertexData[i + 7] = current->TextureCoordinates.X;
                        _vertexData[i + 8] = current->TextureCoordinates.Y;

                        current++;
                    }
                }
            }
            
            glBindBuffer(GL_ARRAY_BUFFER, vbo);

            unsafe
            {
                fixed (void* data = &_vertexData[0])
                {
                    glBufferData(GL_ARRAY_BUFFER, arrSize * sizeof(float), data, GL_DYNAMIC_DRAW);
                }
            }

            glBindVertexArray(_vao);
            
            // Vertex attributes
            glEnableVertexAttribArray(0);
            glVertexAttribPointer(0, 3, GL_FLOAT, false, vertSize * sizeof(float), IntPtr.Zero);
            
            glEnableVertexAttribArray(1);
            glVertexAttribPointer(1, 4, GL_FLOAT, false, vertSize * sizeof(float), new IntPtr(3 * sizeof(float)));
            
            glEnableVertexAttribArray(2);
            glVertexAttribPointer(2, 2, GL_FLOAT, false, vertSize * sizeof(float), new IntPtr(7 * sizeof(float)));
        }
    }
}