namespace Thundershock.Core.Rendering
{
    public abstract class TextureCollection
    {
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