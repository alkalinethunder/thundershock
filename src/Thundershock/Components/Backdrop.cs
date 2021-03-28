using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Components
{
    public class Backdrop : SceneComponent
    {
        public Texture2D Texture { get; set; }

        protected override void OnDraw(GameTime gameTime, SpriteBatch batch)
        {
            if (Texture != null)
            {
                var rect = new Rectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight);

                batch.Begin();
                batch.Draw(Texture, rect, Color.White);
                batch.End();
            }
        }
    }
}