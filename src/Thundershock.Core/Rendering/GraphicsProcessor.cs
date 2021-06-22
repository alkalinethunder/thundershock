using System;
using System.Buffers;
using System.Numerics;

namespace Thundershock.Core.Rendering
{
    public abstract class GraphicsProcessor
    {
        public Matrix4x4 ProjectionMatrix { get; set; } = Matrix4x4.Identity;

        public abstract void SubmitIndices(ReadOnlySpan<int> indices);
        public abstract Rectangle ViewportBounds { get; }
        public abstract Rectangle ScissorRectangle { get; set; }
        public abstract bool EnableScissoring { get; set; }
        public abstract TextureCollection Textures { get; }
        public abstract void Clear(Color color);

        public abstract void DrawPrimitives(PrimitiveType primitiveType, int primitiveStart, int primitiveCount);

        public abstract uint CreateVertexBuffer();
        public abstract void DeleteVertexBuffer(uint vbo);
        public abstract void SubmitVertices(uint vbo, ReadOnlySpan<Vertex> vertices);

        public abstract uint CreateTexture(int width, int height);

        public abstract void UploadTextureData(uint texture, ReadOnlySpan<byte> pixelData, int x, int y, int width, int height);
        public abstract void DeleteTexture(uint texture);
        public abstract void SetViewportArea(int x, int y, int width, int height);
        public abstract uint CreateRenderTarget(uint texture);
        public abstract void DestroyRenderTarget(uint renderTarget);

        public void SetRenderTarget(RenderTarget renderTarget)
        {
            if (renderTarget == null)
                StopUsingRenderTarget();
            else
                UseRenderTarget(renderTarget);
        }

        protected abstract void StopUsingRenderTarget();
        protected abstract void UseRenderTarget(RenderTarget target);

        public abstract uint CreateShaderProgram();
        public abstract void CompileGLSL(uint program, ShaderCompilation type, string glslSource);

        public abstract void SetActiveShaderProgram(uint program);
        
        public abstract void VerifyShaderProgram(uint program);

        public abstract EffectParameter GetEffectParameter(Effect.EffectProgram program, string name);
    }
}