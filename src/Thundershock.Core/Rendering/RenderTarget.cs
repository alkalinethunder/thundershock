namespace Thundershock.Core.Rendering
{
    public abstract class RenderTarget : Texture
    {
        private uint _rtId;
        private GraphicsProcessor _gpu;
        
        public uint RenderTargetId => _rtId;
        
        public RenderTarget(GraphicsProcessor gpu, int width, int height) : base(gpu, width, height)
        {
            _gpu = gpu;
            _rtId = gpu.CreateRenderTarget(Id);
        }

        protected override void OnDisposing()
        {
            _gpu.DestroyRenderTarget(_rtId);
        }
    }
}