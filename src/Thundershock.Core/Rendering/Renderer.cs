using System;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;

namespace Thundershock.Core.Rendering
{
    public class Renderer
    {
        private Effect.EffectProgram _program;
        private bool _isRendering;
        private GraphicsProcessor _gpu;
        private VertexBuffer _vertexBuffer;
        private BasicEffect _defaultEffect;
        
        public bool IsRendering => _isRendering;
        
        public TextureCollection Textures => _gpu.Textures;

        public Matrix4x4 ProjectionMatrix { get; set; } = Matrix4x4.Identity;
        
        public Renderer(GraphicsProcessor gpu)
        {
            _gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
            _vertexBuffer = new VertexBuffer(_gpu);
            _defaultEffect = new BasicEffect(_gpu);
        }

        public void Clear()
        {
            _gpu.Clear(Color.Black);
        }

        public void Clear(Color color)
        {
            _gpu.Clear(color);
        }

        public void Begin(IEffect effect = null)
        {
            ThrowIfNotEnded();

            if (effect != null)
            {
                _program = effect.Programs.First();
            }
            else
            {
                _program = _defaultEffect.Programs.First();
            }

            _program.Apply();
            
            _isRendering = true;
        }

        public void End()
        {
            ThrowIfNotBegun();

            _isRendering = false;
        }

        public void SetRenderTarget(RenderTarget2D renderTarget)
        {
            ThrowIfNotEnded();

            _gpu.SetRenderTarget(renderTarget);
        }

        public void UploadVertices(ReadOnlySpan<Vertex> vertices)
        {
            ThrowIfNotBegun();

            _vertexBuffer.SubmitVertices(vertices);
        }

        public void UploadIndices(ReadOnlySpan<int> indices)
        {
            ThrowIfNotBegun();

            _gpu.SubmitIndices(indices);
        }
        
        public void Draw(PrimitiveType primitive, int offset, int primitiveCount)
        {
            ThrowIfNotBegun();
            
            // Determine the index buffer length based on the primitive type.
            var length = primitive switch
            {
                PrimitiveType.LineStrip => primitiveCount * 2,
                PrimitiveType.TriangleStrip => primitiveCount * 3,
                PrimitiveType.TriangleList => primitiveCount * 3,
                _ => primitiveCount
            };
            
            // Shader parameters.
            _program.Parameters["projection"]?.SetValue(ProjectionMatrix);
            
            // Tell the GPU to draw the primitives.
            _gpu.DrawPrimitives(primitive, offset, primitiveCount);
        }

        private void ThrowIfNotEnded()
        {
            if (_isRendering)
                throw new InvalidOperationException("Rendering has not ended yet - please call End() first.");
        }
        
        private void ThrowIfNotBegun()
        {
            if (!_isRendering)
                throw new InvalidOperationException("Rendering has not started - please call Begin() first.");
        }
    }

    public sealed class VertexBuffer : IDisposable
    {
        private GraphicsProcessor _gpu;
        private uint _gpuID;
        
        public VertexBuffer(GraphicsProcessor gpu)
        {
            _gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
            _gpuID = _gpu.CreateVertexBuffer();
        }

        public void SubmitVertices(ReadOnlySpan<Vertex> vertices)
        {
            _gpu.SubmitVertices(_gpuID, vertices);
        }

        public void Dispose()
        {
            if (_gpu != null)
            {
                _gpu.DeleteVertexBuffer(_gpuID);
                _gpu = null;
                _gpuID = 0;
            }
        }
    }
}