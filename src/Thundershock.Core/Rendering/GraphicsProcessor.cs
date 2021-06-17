using System;
using System.Buffers;

namespace Thundershock.Core.Rendering
{
    public abstract class GraphicsProcessor
    {
        public abstract void Clear(Color color);

        public abstract void DrawPrimitives(PrimitiveType primitiveType, ReadOnlySpan<int> indices, int primitiveCount);

        public abstract uint CreateVertexBuffer();
        public abstract void DeleteVertexBuffer(uint vbo);
        public abstract void SubmitVertices(uint vbo, ReadOnlySpan<Vertex> vertices);
    }
}