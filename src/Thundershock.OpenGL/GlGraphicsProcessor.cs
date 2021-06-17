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
        private GlTextureCollection _textures;

        public override TextureCollection Textures => _textures;
        
        internal GlGraphicsProcessor()
        {
            // Texture colllection
            _textures = new GlTextureCollection(this);
            
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

            var texUniform = glGetUniformLocation(_basicEffect, "ts_textureSampler");
            if (texUniform > -1)
            {
                glUniform1i(texUniform, 0);
            }

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

        public override uint CreateTexture(int width, int height)
        {
            var id = glGenTexture();

            glBindTexture(GL_TEXTURE_2D, id);

            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
            
            glBindTexture(GL_TEXTURE_2D, 0);

            return id;
        }

        public override void UploadTextureData(uint texture, ReadOnlySpan<byte> pixelData, int width, int height)
        {
            glBindTexture(GL_TEXTURE_2D, texture);

            unsafe
            {
                fixed (void* data = pixelData)
                {
                    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, data);
                }
            }

            glBindTexture(GL_TEXTURE_2D, 0);
        }

        public override void DeleteTexture(uint texture)
        {
            glDeleteTexture(texture);
        }
    }

    public sealed class GlTextureCollection : TextureCollection
    {
        private Texture[] _textures;
        
        public GlTextureCollection(GraphicsProcessor gpu) : base(gpu)
        {
            _textures = new Texture[32];
        }

        public override int Count => _textures.Length;
        protected override Texture GetTexture(int index)
        {
            return _textures[index];
        }

        protected override void BindTexture(int index, Texture texture)
        {
            glActiveTexture(index);
            if (texture == null)
            {
                glBindTexture(GL_TEXTURE_2D, 0);
            }
            else
            {
                glBindTexture(GL_TEXTURE_2D, texture.Id);
            }

            _textures[index] = texture;
        }
    }
}