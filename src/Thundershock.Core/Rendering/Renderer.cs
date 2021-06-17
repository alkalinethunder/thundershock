using System;

namespace Thundershock.Core.Rendering
{
    public class Renderer
    {
        private bool _isRendering;
        private GraphicsProcessor _gpu;
        private VertexBuffer _vertexBuffer;
        
        public bool IsRendering => _isRendering;
        
        public Renderer(GraphicsProcessor gpu)
        {
            _gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
            _vertexBuffer = new VertexBuffer(_gpu);
        }

        public void Clear()
        {
            _gpu.Clear(Color.Black);
        }

        public void Clear(Color color)
        {
            _gpu.Clear(color);
        }

        public void Begin()
        {
            ThrowIfNotEnded();

            _isRendering = true;
        }

        public void End()
        {
            ThrowIfNotBegun();

            _isRendering = false;
        }

        public void Draw(PrimitiveType primitive, Vertex[] vertices, int[] indexBuffer, int offset, int primitiveCount)
        {
            ThrowIfNotBegun();
            
            // Determine the index buffer length based on the primitive type.
            var length = primitive switch
            {
                PrimitiveType.LineStrip => primitiveCount * 2,
                PrimitiveType.TriangleStrip => primitiveCount * 3,
                _ => primitiveCount
            };
            
            // Create a span of the indexes we want to upload.
            var indexSpan = new ReadOnlySpan<int>(indexBuffer, offset, length);
         
            // Submit vertices to the vertex buffer
            _vertexBuffer.SubmitVertices(vertices);
            
            // Tell the GPU to draw the primitives.
            _gpu.DrawPrimitives(primitive, indexSpan, primitiveCount);
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