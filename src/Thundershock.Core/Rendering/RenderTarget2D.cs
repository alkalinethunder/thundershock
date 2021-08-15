namespace Thundershock.Core.Rendering
{
    public sealed class RenderTarget2D : RenderTarget
    {
        public RenderTarget2D(GraphicsProcessor gpu, int width, int height, TextureFilteringMode filterMode, DepthFormat depthFormat) : base(gpu, width, height, filterMode, depthFormat)
        {
            
        }
    }
}