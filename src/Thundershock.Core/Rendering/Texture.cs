using System;

namespace Thundershock.Core.Rendering
{
    public abstract class Texture : IDisposable
    {
        private uint _id;
        
        private GraphicsProcessor _gpu;
        private int _width;
        private int _height;

        public int Width => _width;
        public int Height => _height;

        public uint Id => _id;

        private Rectangle _bounds;

        public Rectangle Bounds => _bounds;
        
        public Texture(GraphicsProcessor gpu, int width, int height)
        {
            if (width <= 0)
                throw new InvalidOperationException("Texture width must be above zero.");

            if (height < 0)
                throw new InvalidOperationException("Texture height must be above zero.");

            _width = width;
            _height = height;
            _gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
            
            // create the texture on the GPU
            _id = _gpu.CreateTexture(width, height);

            _bounds = new Rectangle(0, 0, Width, Height);
        }

        public void Dispose()
        {
            OnDisposing();
            _gpu.DeleteTexture(_id);
        }

        public void Upload(ReadOnlySpan<byte> pixelData, Rectangle? bounds = null)
        {
            var trueBounds = bounds ?? this.Bounds;
            _gpu.UploadTextureData(_id, pixelData, (int) trueBounds.X, (int) trueBounds.Y, (int) trueBounds.Width,
                (int) trueBounds.Height);
        }
        
        protected virtual void OnDisposing() {}
    }
}