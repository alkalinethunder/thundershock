using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock.Components
{
    public class Sprite
    {
        public Color Color = Color.White;
        public Texture2D Texture = null;
        public Vector2 Size = new Vector2(32, 32);
        public Vector2 Pivot = new Vector2(0.5f, 0.5f);
    }
}