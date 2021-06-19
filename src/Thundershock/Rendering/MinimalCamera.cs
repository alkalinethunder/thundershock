using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Core;

namespace Thundershock.Rendering
{
    public sealed class MinimalCamera : Camera
    {
        public override Microsoft.Xna.Framework.Rectangle ViewportBounds
        {
            get
            {
                if (EntryPoint.CurrentApp is GameAppBase game)
                {
                    return game.GraphicsDevice.Viewport.Bounds;
                }

                return Microsoft.Xna.Framework.Rectangle.Empty;
            }
        }

        public override Matrix GetRenderTransform(GraphicsDevice gfx)
        {
            return Matrix.Identity;
        }
    }
}
