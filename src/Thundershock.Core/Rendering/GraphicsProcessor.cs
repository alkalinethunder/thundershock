using System;
using System.Buffers;
using System.Numerics;

namespace Thundershock.Core.Rendering
{
    public abstract class GraphicsProcessor
    {
        public Matrix4x4 ProjectionMatrix { get; set; } = Matrix4x4.Identity;

        public abstract TextureCollection Textures { get; }
        public abstract void Clear(Color color);

        public abstract void DrawPrimitives(PrimitiveType primitiveType, ReadOnlySpan<int> indices, int primitiveCount);

        public abstract uint CreateVertexBuffer();
        public abstract void DeleteVertexBuffer(uint vbo);
        public abstract void SubmitVertices(uint vbo, ReadOnlySpan<Vertex> vertices);

        public abstract uint CreateTexture(int width, int height);

        public abstract void UploadTextureData(uint texture, ReadOnlySpan<byte> pixelData, int width, int height);
        public abstract void DeleteTexture(uint texture);
        public abstract void SetViewportArea(int x, int y, int width, int height);
    }
}