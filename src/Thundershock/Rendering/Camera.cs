using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock.Rendering
{
    public abstract class Camera
    {
        public int ViewportWidth { get; set; } = 1600;
        public int ViewportHeight { get; set; } = 900;

        public virtual Rectangle ViewportBounds
            => new Rectangle(0, 0, ViewportWidth, ViewportHeight);

        public abstract Matrix4x4 GetRenderTransform(GraphicsProcessor gfx);
    }
}