namespace Thundershock.Core.Rendering
{
    public abstract class RenderTarget : Texture
    {
        private uint _rtID;
        private GraphicsProcessor _gpu;
        
        public uint RenderTargetId => _rtID;
        
        public RenderTarget(GraphicsProcessor gpu, int width, int height) : base(gpu, width, height)
        {
            _gpu = gpu;
            _rtID = gpu.CreateRenderTarget(this.Id);
        }

        protected override void OnDisposing()
        {
            _gpu.DestroyRenderTarget(_rtID);
        }
    }
}