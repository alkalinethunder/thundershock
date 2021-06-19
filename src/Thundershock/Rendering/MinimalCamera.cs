using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock.Rendering
{
    public sealed class MinimalCamera : Camera
    {
        public override Rectangle ViewportBounds
        {
            get
            {
                return Rectangle.Empty;
            }
        }

        public override Matrix4x4 GetRenderTransform(GraphicsProcessor gfx)
        {
            return Matrix4x4.Identity;
        }
    }
}
