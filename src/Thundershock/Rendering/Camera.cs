using System.Threading.Tasks.Dataflow;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Rendering
{
    public abstract class Camera
    {
        public int ViewportWidth { get; set; } = 1600;
        public int ViewportHeight { get; set; } = 900;

        public virtual Rectangle ViewportBounds
            => new Rectangle(0, 0, ViewportWidth, ViewportHeight);
        
        public abstract Matrix GetRenderTransform(GraphicsDevice gfx);
    }
}