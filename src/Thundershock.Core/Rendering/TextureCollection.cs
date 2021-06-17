using System;

namespace Thundershock.Core.Rendering
{
    public abstract class TextureCollection
    {
        private GraphicsProcessor _gpu;
        
        public TextureCollection(GraphicsProcessor gpu)
        {
            _gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
        }

        public abstract int Count { get; }
        
        public Texture this[int index]
        {
            get => GetTexture(index);
            set => BindTexture(index, value);
        }

        protected abstract Texture GetTexture(int index);
        protected abstract void BindTexture(int index, Texture texture);
    }
}