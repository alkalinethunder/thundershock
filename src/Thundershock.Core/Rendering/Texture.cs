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
        }

        public void Dispose()
        {
            OnDisposing();
            _gpu.DeleteTexture(_id);
        }

        public void Upload(ReadOnlySpan<byte> pixelData)
        {
            _gpu.UploadTextureData(_id, pixelData, _width, _height);
        }
        
        protected virtual void OnDisposing() {}
    }
}