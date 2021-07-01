using System;
using FontStashSharp.Interfaces;
using Thundershock.Core.Rendering;

namespace Thundershock.Core.Fonts
{
    internal class FontTextureManager : ITexture2DManager
    {
        private GraphicsProcessor _gpu;

        public FontTextureManager(GraphicsProcessor gpu)
        {
            _gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
        }
        
        public object CreateTexture(int width, int height)
        {
            return new Texture2D(_gpu, width, height);
        }

        public void SetTextureData(object texture, System.Drawing.Rectangle bounds, byte[] data)
        {
            if (texture is Texture2D tsTexture)
            {
                var tsRect = new Rectangle(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                tsTexture.Upload(data, tsRect);
            }
        }

    }
}