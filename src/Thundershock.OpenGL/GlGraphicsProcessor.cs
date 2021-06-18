using System;
using System.Numerics;
using Silk.NET.OpenGL;
using Thundershock.Core;
using Thundershock.Core.Debugging;
using Thundershock.Core.Rendering;
using Thundershock.Debugging;

using Silk.NET.OpenGL;
using Thundershock.OpenGL;
using PrimitiveType = Thundershock.Core.Rendering.PrimitiveType;

namespace Thundershock.OpenGL
{
    public sealed class GlGraphicsProcessor : GraphicsProcessor
    {
        private uint _program;
        private GL _gl;
        private uint _fbo;
        private int _viewportX;
        private int _viewportY;
        private int _viewportW;
        private int _viewportH;
        private float[] _matrixBuffer = new float[4 * 4];
        private float[] _vertexData = Array.Empty<float>();
        private uint _vertexBuffer;
        private uint _indexBuffer;
        private uint _vao;
        private GlTextureCollection _textures;

        public override TextureCollection Textures => _textures;
        
        internal GlGraphicsProcessor(GL glContext)
        {
            _gl = glContext;
            
            // Texture colllection
            _textures = new GlTextureCollection(this, _gl);
            
            // Vertex array object.
            _vao = _gl.GenVertexArray();
            _gl.BindVertexArray(_vao);

            // generate the vertex buffer and index buffer objects.
            _vertexBuffer = _gl.GenBuffer();
            _indexBuffer = _gl.GenBuffer();
            
            // bind the index buffer
            _gl.BindBuffer(GLEnum.ArrayBuffer, _vertexBuffer);
            _gl.BindBuffer(GLEnum.ElementArrayBuffer, _indexBuffer);
        }


        public override void Clear(Color color)
        {
            var vec4 = color.ToVector4();
            _gl.ClearColor(vec4.X, vec4.Y, vec4.Z, vec4.W);
            _gl.Clear((uint) GLEnum.ColorBufferBit | (uint) GLEnum.DepthBufferBit);
        }

                public override void DrawPrimitives(PrimitiveType primitiveType, ReadOnlySpan<int> indices, int primitiveCount)
                {
                    _gl.UseProgram(_program);
                    _gl.BindBuffer(GLEnum.ElementArrayBuffer, _indexBuffer);
                    _gl.BufferData(GLEnum.ElementArrayBuffer, indices, GLEnum.StaticDraw);
                    
                    var type = primitiveType switch
                    {
                        PrimitiveType.LineStrip => Silk.NET.OpenGL.PrimitiveType.LineStrip,
                        PrimitiveType.TriangleStrip => Silk.NET.OpenGL.PrimitiveType.TriangleStrip,
                        _ => throw new NotSupportedException()
                    };
        
                    var name = "tex";
                    ;
                    
                    for (var i = 0; i < 32; i++)
                    {
                        name = "tex" + i.ToString();
        
                        var location = _gl.GetUniformLocation(_program, name);
        
                        if (location > -1)
                        {
                            _gl.Uniform1(location, i);
                        }
                    }
                    
                    unsafe
                    {
                        void* nullptr = null;
                        _gl.DrawElements(type, (uint) indices.Length, GLEnum.UnsignedInt, nullptr);
                    }
                }

        public override uint CreateVertexBuffer()
        {
            var vbo = _gl.GenBuffer();
            return vbo;
        }

        public override void DeleteVertexBuffer(uint vbo)
        {
            _gl.DeleteBuffer(vbo);
        }

        public override void SubmitVertices(uint vbo, ReadOnlySpan<Vertex> vertices)
        {
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

            _gl.BindBuffer(GLEnum.ArrayBuffer, vbo);

            unsafe
            {
                fixed (void* ptr = _vertexData)
                {
                    _gl.BufferData(GLEnum.ArrayBuffer, new UIntPtr((uint) (_vertexData.Length * sizeof(float))), ptr,
                        GLEnum.StaticDraw);
                }
            }

            _gl.BindVertexArray(_vao);

            // Vertex attributes
            unsafe
            {
                void* pptr = (void*) IntPtr.Zero;
                void* cptr = (void*) new IntPtr(sizeof(float) * 3);
                void* tptr = (void*) new IntPtr(sizeof(float) * 7);

                _gl.EnableVertexAttribArray(0);
                _gl.VertexAttribPointer(0, 3, GLEnum.Float, false, (uint) vertSize * sizeof(float), pptr);

                _gl.EnableVertexAttribArray(1);
                _gl.VertexAttribPointer(1, 4, GLEnum.Float, false, (uint) vertSize * sizeof(float),
                    cptr);

                _gl.EnableVertexAttribArray(2);
                _gl.VertexAttribPointer(2, 2, GLEnum.Float, false, (uint) vertSize * sizeof(float),
                    tptr);
            }
        }

        public override uint CreateTexture(int width, int height)
        {
            var id = _gl.GenTexture();

            _gl.BindTexture(GLEnum.Texture2D, id);

            _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int) GLEnum.Nearest);
            _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int) GLEnum.Nearest);
            _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int) GLEnum.ClampToEdge);
            _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int) GLEnum.ClampToEdge);

            unsafe
            {
                _gl.TexImage2D(GLEnum.Texture2D, 0, (int) GLEnum.Rgba8, (uint) width, (uint) height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, null);
            }
            
            _gl.BindTexture(GLEnum.Texture2D, 0);

            return id;
        }

        public override void UploadTextureData(uint texture, ReadOnlySpan<byte> pixelData, int width, int height)
        {
            _gl.BindTexture(GLEnum.Texture2D, texture);

            unsafe
            {
                fixed (void* data = pixelData)
                {
                    _gl.TexImage2D(GLEnum.Texture2D, 0, (int) GLEnum.Rgba8, (uint) width, (uint) height, 0, GLEnum.Rgba,
                        GLEnum.UnsignedByte, data);
                }
            }

            _gl.BindTexture(GLEnum.Texture2D, 0);
        }

        public override void DeleteTexture(uint texture)
        {
            _gl.DeleteTexture(texture);
        }
        
        public override void SetViewportArea(int x, int y, int width, int height)
        {
            _viewportX = x;
            _viewportY = y;
            _viewportW = width;
            _viewportH = height;

            if (_fbo == 0)
            {
                _gl.Viewport(x, y, (uint) width, (uint) height);
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
            var fbo = _gl.GenFramebuffer();
            
            // Return it.
            return fbo;
        }

        public override void DestroyRenderTarget(uint renderTarget)
        {
            _gl.DeleteFramebuffer(renderTarget);
        }

        protected override void UseRenderTarget(RenderTarget target)
        {
            _fbo = target.RenderTargetId;
            _gl.BindFramebuffer(GLEnum.Framebuffer, target.RenderTargetId);
            
            // Attach it to the texture.
            _gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, target.Id, 0);
            
            _gl.DrawBuffer(GLEnum.ColorAttachment0);
            var result = _gl.CheckFramebufferStatus(GLEnum.Framebuffer);
            if (result != GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException("Failed to perform a render target switch.");
            }
            _gl.Viewport(0, 0, (uint) target.Width, (uint) target.Height);
        }

        protected override void StopUsingRenderTarget()
        {
            _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            _gl.Viewport(_viewportX, _viewportY, (uint) _viewportW, (uint) _viewportH);
            _fbo = 0;
        }

        public override uint CreateShaderProgram()
        {
            return _gl.CreateProgram();
        }

        public override void CompileGLSL(uint program, ShaderCompilation type, string glslSource)
        {
            var shader = _gl.CreateShader(type == ShaderCompilation.VertexShader ? GLEnum.VertexShader : GLEnum.FragmentShader);
            _gl.ShaderSource(shader, glslSource);
            _gl.CompileShader(shader);
            
            int result = 0;
            _gl.GetShader(shader, GLEnum.CompileStatus, out result);
            
            var log = _gl.GetShaderInfoLog(shader);
            if (!string.IsNullOrEmpty(log))
            {
                var lines = log.Split(Environment.NewLine);
                foreach (var line in lines)
                {
                    if (line.Contains("warning"))
                        Logger.GetLogger().Log(line, LogLevel.Warning);
                    else if (line.Contains("error"))
                        Logger.GetLogger().Log(line, LogLevel.Error);
                    else
                        Logger.GetLogger().Log(line);
                }
            }

            if (result == (int) GLEnum.False)
            {
                Logger.GetLogger().Log("Shader compilation failed.", LogLevel.Error);
                _gl.DeleteShader(shader);
                return;
            }

            _gl.AttachShader(program, shader);
        }

        public override void VerifyShaderProgram(uint program)
        {
            _gl.LinkProgram(program);
            _gl.ValidateProgram(program);
        }

        public override void SetActiveShaderProgram(uint program)
        {
            _gl.UseProgram(program);
            _program = program;
        }

        public override EffectParameter GetEffectParameter(Effect.EffectProgram program, string name)
        {
            var location = _gl.GetUniformLocation(program.Id, name);
            if (location > -1)
                return new GLEffectParameter(program.Id, name, location, _gl);
            return null;
        }
    }
}