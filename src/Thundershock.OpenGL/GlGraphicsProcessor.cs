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
        private int[] _programTextureUnits;
        private bool _programChangedSinceLastDrawCall = false;
        
        private bool _vaoReady = false;
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
        private bool _scissor;
        private Rectangle _scissorRect;
        
        public override TextureCollection Textures => _textures;

        public override Rectangle ScissorRectangle
        {
            get => _scissorRect;
            set => _scissorRect = value;
        }

        public override bool EnableScissoring
        {
            get => _scissor;
            set => _scissor = value;
        }
        
        public override Rectangle ViewportBounds
        {
            get
            {
                var viewport = new int[4];
                unsafe
                {
                    fixed (int* ptr = viewport)
                        _gl.GetInteger(GLEnum.Viewport, ptr);
                }

                return new Rectangle(viewport[0], viewport[1], viewport[2], viewport[3]);
            }
        }
        
        internal GlGraphicsProcessor(GL glContext)
        {
            _gl = glContext;
         
            // Number of texture units a shader can access.
            _programTextureUnits = new int[_gl.GetInteger(GLEnum.MaxTextureImageUnits)];

            // Texture colllection
            _textures = new GlTextureCollection(this, _gl);
            
            // Vertex array object.
            CreateVertexArray();
            
            // generate the vertex buffer and index buffer objects.
            _vertexBuffer = this.CreateVertexBuffer();
            _indexBuffer = _gl.CreateBuffer();
            
            // bind the index buffer
            _gl.VertexArrayElementBuffer(_vao, _indexBuffer);
        }


        public override void Clear(Color color)
        {
            var vec4 = color.ToVector4();
            _gl.ClearColor(vec4.X, vec4.Y, vec4.Z, vec4.W);
            _gl.Clear((uint) GLEnum.ColorBufferBit | (uint) GLEnum.DepthBufferBit);
        }

        public override void SubmitIndices(ReadOnlySpan<int> indices)
        {
            // Yay for direct state access!
            _gl.NamedBufferData(_indexBuffer, UIntPtr.Zero + (sizeof(int) * indices.Length), indices,
                GLEnum.StaticDraw);
        }

        public override void PrepareRender()
        {
            // Back-face culling - improves performance because we only need to render what we can
            // actually see.
            // TODO: fix this so that the engine can enable/disable/configure it.
            _gl.CullFace(GLEnum.Front);
            _gl.Enable(GLEnum.CullFace);

            // Bind to the vertex array object.
            _gl.BindVertexArray(_vao);
            
            // TODO: Let the engine control blending.
            _gl.Enable(GLEnum.Blend);
            _gl.BlendFunc(GLEnum.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            
        }
        
        public override void DrawPrimitives(PrimitiveType primitiveType, int primitiveStart, int primitiveCount)
        {
            _gl.UseProgram(_program);
            
            var type = primitiveType switch
            {
                PrimitiveType.LineStrip => Silk.NET.OpenGL.PrimitiveType.LineStrip,
                PrimitiveType.TriangleStrip => Silk.NET.OpenGL.PrimitiveType.TriangleStrip,
                PrimitiveType.TriangleList => Silk.NET.OpenGL.PrimitiveType.Triangles,
                _ => throw new NotSupportedException()
            };

            var primitiveSize = primitiveType switch
            {
                PrimitiveType.LineStrip => 1,
                PrimitiveType.TriangleStrip => 3,
                PrimitiveType.TriangleList => 3,
                _ => throw new NotSupportedException()
            };


            if (_programChangedSinceLastDrawCall)
            {
                for (var i = 0; i < _programTextureUnits.Length; i++)
                {
                    var name = "tex" + i.ToString();
                    var location = _gl.GetUniformLocation(_program, name);

                    _programTextureUnits[i] = location;
                }

                _programChangedSinceLastDrawCall = false;
            }
            
            // Set up texture units for the program
            for (var i = 0; i < _programTextureUnits.Length; i++)
            {
                var location = _programTextureUnits[i];
                if (location < 0)
                    continue;
                
                _gl.Uniform1(location, i);
            }

            // Scissor testing
            if (_scissor)
                _gl.Enable(GLEnum.ScissorTest);
            else
                _gl.Disable(GLEnum.ScissorTest);

            _gl.Scissor((int) _scissorRect.Left, (int) (_viewportH - _scissorRect.Height - _scissorRect.Y), (uint) _scissorRect.Width,
                (uint) _scissorRect.Height);
            
            
            
            unsafe
            {
                nuint nullptr = UIntPtr.Zero;
                _gl.DrawElements(type, (uint) (primitiveCount * primitiveSize), GLEnum.UnsignedInt,
                    (void*) (nullptr + (uint) (int) (primitiveStart * sizeof(uint))));
            }
        }

        public override void EndRender()
        {
            _gl.BindVertexArray(0);
        }
        
        public override uint CreateVertexBuffer()
        {
            var vbo = _gl.CreateBuffer();

            // Set up attribute bindings.
            _gl.VertexArrayAttribBinding(_vao, 0, 0);
            _gl.VertexArrayAttribBinding(_vao, 1, 1);
            _gl.VertexArrayAttribBinding(_vao, 2, 2);

            var posSize = sizeof(float) * 3;
            var colorSize = sizeof(float) * 4;
            var texSize = sizeof(float) * 2;

            var vertSize = (uint) (posSize + colorSize + texSize);
            
            _gl.VertexArrayAttribFormat(_vao, 0, 3, GLEnum.Float, false, 0);
            _gl.VertexArrayAttribFormat(_vao, 1, 4, GLEnum.Float, false, (uint) 0);
            _gl.VertexArrayAttribFormat(_vao, 2, 2, GLEnum.Float, false, (uint) 0);

            // Set up the vertex array attributes.
            _gl.VertexArrayVertexBuffer(_vao, 0, vbo, IntPtr.Zero, vertSize);
            _gl.VertexArrayVertexBuffer(_vao, 1, vbo, IntPtr.Zero + posSize, vertSize);
            _gl.VertexArrayVertexBuffer(_vao, 2, vbo, IntPtr.Zero + posSize + colorSize, vertSize);

            // Enable the attributes.
            _gl.EnableVertexArrayAttrib(_vao, 0);
            _gl.EnableVertexArrayAttrib(_vao, 1);
            _gl.EnableVertexArrayAttrib(_vao, 2);
            
            return vbo;
        }

        public override void DeleteVertexBuffer(uint vbo)
        {
            _gl.DeleteBuffer(vbo);
        }

        public override void SubmitVertices(ReadOnlySpan<Vertex> vertices)
        {
            unsafe
            {
                var posSize = 3;
                var colorSize = 4;
                var texCoordSize = 2;

                var vertSize = posSize + colorSize + texCoordSize;
                var arrSize = vertSize * vertices.Length;

                // Only resize if we need more space - this saves performance.
                if (_vertexData.Length < arrSize)
                    Array.Resize(ref _vertexData, arrSize);

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

                _gl.NamedBufferData<float>(_vertexBuffer, UIntPtr.Zero + (arrSize * sizeof(float)),
                    _vertexData.AsSpan(0, arrSize), GLEnum.StaticDraw);

                // Vertex attributes
                /*void* pptr = (void*) IntPtr.Zero;
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
                */
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

        public override void UploadTextureData(uint texture, ReadOnlySpan<byte> pixelData, int x, int y, int width, int height)
        {
            var checkedSize = (((width) * (height)) * 4);
            if (checkedSize > pixelData.Length)
                throw new InvalidOperationException(
                    $"Texture pixel data length mismatch. Expected: {checkedSize}, instead got {pixelData.Length}.");
            
            _gl.BindTexture(GLEnum.Texture2D, texture);

            unsafe
            {
                fixed (void* data = pixelData)
                {
                    _gl.TexSubImage2D(GLEnum.Texture2D, 0, x, y, (uint) width, (uint) height, GLEnum.Rgba, GLEnum.UnsignedByte,
                        data);
                    
                    // _gl.TexImage2D(GLEnum.Texture2D, 0, (int) GLEnum.Rgba8, (uint) width, (uint) height, 0, GLEnum.Rgba,
                        // GLEnum.UnsignedByte, data);
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
            if (_program != program)
            {
                _programChangedSinceLastDrawCall = true;
            }
            
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

        private void CreateVertexArray()
        {
            // Create the actual vertex array.
            _vao = _gl.CreateVertexArray();
            
            // Attribute formats.
            var posSize = sizeof(float) * 3;
            var colorSize = sizeof(float) * 4;
            var texSize = sizeof(float) * 2;
        }
    }
}