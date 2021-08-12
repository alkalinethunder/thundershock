using System.Collections.Generic;

namespace Thundershock.Core.Rendering
{
    public abstract class TextureCollection
    {
        private Dictionary<int, Texture> _indexCache = new();
        
        public abstract int Count { get; }
        
        public Texture this[int index]
        {
            get => GetTexture(index);
            set
            {
                // If we're about to get a null texture, then we'll unbind that texture unit if there's a texture in it.
                if (value == null)
                {
                    if (_indexCache.ContainsKey(index))
                    {
                        _indexCache.Remove(index);
                        BindTexture(index, null);
                    }

                    return;
                }
                
                // If there wasn't a texture in the texture unit before, there will be now.
                if (!_indexCache.ContainsKey(index))
                {
                    _indexCache.Add(index, value);
                    BindTexture(index, value);
                    return;
                }

                // Re-bind the texture unit if the texture has changed.
                if (_indexCache[index] != value)
                {
                    _indexCache[index] = value;
                    BindTexture(index, value);
                }
            }
        }

        protected abstract Texture GetTexture(int index);
        protected abstract void BindTexture(int index, Texture texture);
    }
}