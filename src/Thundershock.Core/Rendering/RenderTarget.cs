namespace Thundershock.Core.Rendering
{
    public abstract class RenderTarget : Texture
    {
        private uint _rtId;
        private GraphicsProcessor _gpu;
        
        public DepthFormat DepthFormat { get; }
        
        public uint RenderTargetId => _rtId;
        
        public RenderTarget(GraphicsProcessor gpu, int width, int height, TextureFilteringMode filterMode, DepthFormat depthFormat) : base(gpu, width, height, filterMode)
        {
            DepthFormat = depthFormat;
            
            _gpu = gpu;
            _rtId = gpu.CreateRenderTarget(Id, (uint) width, (uint) height, depthFormat);
        }

        protected override void OnDisposing()
        {
            _gpu.DestroyRenderTarget(_rtId);
        }
    }
}