using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

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
        
        public Texture(GraphicsProcessor gpu, int width, int height, TextureFilteringMode filterMode)
        {
            if (width <= 0)
                throw new InvalidOperationException("Texture width must be above zero.");

            if (height < 0)
                throw new InvalidOperationException("Texture height must be above zero.");

            _width = width;
            _height = height;
            _gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
            
            // create the texture on the GPU
            _id = _gpu.CreateTexture(width, height, filterMode);

            _bounds = new Rectangle(0, 0, Width, Height);
        }

        public void Dispose()
        {
            OnDisposing();
            _gpu.DeleteTexture(_id);
        }

        public void Upload(ReadOnlySpan<byte> pixelData, Rectangle? bounds = null)
        {
            var trueBounds = bounds ?? Bounds;
            _gpu.UploadTextureData(_id, pixelData, (int) trueBounds.X, (int) trueBounds.Y, (int) trueBounds.Width,
                (int) trueBounds.Height);
        }

        public byte[] Download(Rectangle? bounds = null)
        {
            var trueBounds = bounds ?? Bounds;
            return _gpu.DownloadTextureData(_id, (int) trueBounds.X, (int) trueBounds.Y, (int) trueBounds.Width,
                (int) trueBounds.Height);
        }
        
        public void SavePng(Stream outputStream)
        {
            var hopefullyMoreThan11Pixels = Download();

            using var bitmap = new Bitmap((int) Width, (int) Height);

            var lck = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            for (var i = 0; i < hopefullyMoreThan11Pixels.Length; i += 4)
            {
                var r = hopefullyMoreThan11Pixels[i];
                var b = hopefullyMoreThan11Pixels[i + 2];

                hopefullyMoreThan11Pixels[i] = b;
                hopefullyMoreThan11Pixels[i + 2] = r;
            }

            Marshal.Copy(hopefullyMoreThan11Pixels, 0, lck.Scan0, hopefullyMoreThan11Pixels.Length);

            bitmap.UnlockBits(lck);

            bitmap.Save(outputStream, ImageFormat.Png);

            bitmap.Dispose();
        }
        
        protected virtual void OnDisposing() {}
    }
}