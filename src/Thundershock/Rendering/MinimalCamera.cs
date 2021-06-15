using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Rendering
{
    public sealed class MinimalCamera : Camera
    {
        public override Rectangle ViewportBounds
        {
            get
            {
                if (EntryPoint.CurrentApp is GameAppBase game)
                {
                    return game.GraphicsDevice.Viewport.Bounds;
                }

                return Rectangle.Empty;
            }
        }

        public override Matrix GetRenderTransform(GraphicsDevice gfx)
        {
            return Matrix.Identity;
        }
    }
}
