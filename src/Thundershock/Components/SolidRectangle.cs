using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Components
{
    public class SolidRectangle : SceneComponent
    {
        public Color Color { get; set; } = Color.White;
        public Vector2 Position { get; set; }
        public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector2 Size { get; set; } = new Vector2(50, 50);

        protected override void OnDraw(GameTime gameTime, SpriteBatch batch)
        {
            var rect = new Rectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight);

            var origin = rect.Location.ToVector2() + (rect.Size.ToVector2() * Origin);

            var pivot = Size * Pivot;

            var pos = origin - pivot + Position;

            rect.X = (int) pos.X;
            rect.Y = (int) pos.Y;
            rect.Width = (int) Size.X;
            rect.Height = (int) Size.Y;

            batch.Begin();
            batch.Draw(Game.White, rect, Color);
            batch.End();
        }
    }
}