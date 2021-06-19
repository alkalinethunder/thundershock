using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock.Rendering
{
    public class Camera2D : Camera
    {
        public AspectRatioMode AspectRatioMode { get; set; } = AspectRatioMode.ScaleVertically;

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
