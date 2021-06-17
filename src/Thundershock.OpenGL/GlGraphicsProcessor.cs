using System;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Rendering;

using static OpenGL.GL;

namespace Thundershock.OpenGL
{
    public sealed class GlGraphicsProcessor : GraphicsProcessor
    {
        private uint _fbo;
        private int _viewportX;
        private int _viewportY;
        private int _viewportW;
        private int _viewportH;
        private float[] _matrixBuffer = new float[4 * 4];
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
            var camProjectionUniform = glGetUniformLocation(_basicEffect, "ts_cam_projectionMatrix");
            
            if (texUniform > -1)
            {
                glUniform1i(texUniform, 0);
            }

            if (camProjectionUniform > -1)
            {
                SubmitMatrix(ProjectionMatrix);
                glUniformMatrix4fv(camProjectionUniform, 1, false, _matrixBuffer);
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

                        // WORD OF WARNING:
                        //
                        // OpenGL expects texture coordinates to be (0-1) with (0, 0) being the bottom left.
                        // However when working with Thundershock's 2D batcher, it'll use an off-center orthographic
                        // projection matrix to transform pixel positions into vertex space.  That matrix will do
                        // a bunch of math I don't understand that ultimately ends up meaning that the 2D renderer
                        // will consider (0,0) to be the top-left texture coordinate. This is great for 2D as it's
                        // more logical. But it may confuse you if you spend a lot of time in both renderers.
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

            unsafe
            {
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, null);
            }
            
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
        
        public override void SetViewportArea(int x, int y, int width, int height)
        {
            _viewportX = x;
            _viewportY = y;
            _viewportW = width;
            _viewportH = height;

            if (_fbo != 0)
            {
                glViewport(x, y, width, height);
            }
        }

        private void SubmitMatrix(Matrix4x4 matrix)
        {
            _matrixBuffer[0] = matrix.M11;
            _matrixBuffer[1] = matrix.M21;
            _matrixBuffer[2] = matrix.M31;
            _matrixBuffer[3] = matrix.M41;
            
            _matrixBuffer[4] = matrix.M12;
            _matrixBuffer[5] = matrix.M22;
            _matrixBuffer[6] = matrix.M32;
            _matrixBuffer[7] = matrix.M42;
            
            _matrixBuffer[8] = matrix.M13;
            _matrixBuffer[9] = matrix.M23;
            _matrixBuffer[10] = matrix.M33;
            _matrixBuffer[11] = matrix.M43;
            
            _matrixBuffer[12] = matrix.M14;
            _matrixBuffer[13] = matrix.M24;
            _matrixBuffer[14] = matrix.M34;
            _matrixBuffer[15] = matrix.M44;
        }

        public override uint CreateRenderTarget(uint texture)
        {
            // Create a framebuffer.
            var fbo = glGenFramebuffer();
            
            // Return it.
            return fbo;
        }

        public override void DestroyRenderTarget(uint renderTarget)
        {
            glDeleteFramebuffer(renderTarget);
        }

        protected override void UseRenderTarget(RenderTarget target)
        {
            _fbo = target.RenderTargetId;
            glBindFramebuffer(GL_FRAMEBUFFER, target.RenderTargetId);
            
            // Attach it to the texture.
            glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, target.Id, 0);
            
            glDrawBuffer(GL_COLOR_ATTACHMENT0);
            var result = glCheckFramebufferStatus(GL_FRAMEBUFFER);
            if (result != GL_FRAMEBUFFER_COMPLETE)
            {
                throw new InvalidOperationException("Failed to perform a render target switch.");
            }
            glViewport(0, 0, target.Width, target.Height);
        }

        protected override void StopUsingRenderTarget()
        {
            glBindFramebuffer(GL_FRAMEBUFFER, 0);
            glViewport(_viewportX, _viewportY, _viewportW, _viewportH);
            _fbo = 0;
        }
    }
}