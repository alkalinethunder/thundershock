using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock.Components
{
    public class Backdrop : SceneComponent
    {
        public Texture2D Texture { get; set; }

        protected override void OnDraw(GameTime gameTime, Renderer2D batch)
        {
            /* if (Texture != null)
            {
                var rect = batch.ViewportBounds;

                batch.Begin();
                batch.FillRectangle(rect, Color.White, Texture);
                batch.End();
            }*/
        }
    }
}