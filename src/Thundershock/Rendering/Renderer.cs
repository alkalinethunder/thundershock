using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Rendering
{
    public class Renderer
    {
        private SpriteBatch _batch;
        private Camera _camera;
        private Texture2D _white;

        public Renderer(Texture2D white, SpriteBatch batcher, Camera camera)
        {
            _batch = batcher;
            _white = white;
            _camera = camera;
        }

        public Rectangle ViewportBounds
            => _camera.ViewportBounds;
        
        public void Begin()
        {
            var transform = _camera.GetRenderTransform(_batch.GraphicsDevice);
            var viewWidth = _camera.ViewportWidth;
            var viewHeight = _camera.ViewportHeight;
            
            _batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, transformMatrix: transform);
        }

        public void End()
        {
            _batch.End();
        }

        public void FillRectangle(Rectangle rect, Color color)
            => FillRectangle(rect, color, _white);

        public void FillRectangle(Rectangle rect, Color color, Texture2D texture)
            => FillRectangle(rect.Location.ToVector2(), rect.Size.ToVector2(), color, texture);

        public void FillRectangle(Vector2 position, Vector2 size, Color color)
            => FillRectangle(position, size, color, _white);

        public void FillRectangle(Vector2 position, Vector2 size, Color color, Texture2D texture, SpriteEffects effects = SpriteEffects.None)
        {
            _batch.Draw(texture, new Rectangle((int) position.X, (int) position.Y, (int) size.X, (int) size.Y), null,
                color, 0, Vector2.Zero, effects, 0);
        }

        public void DrawString(SpriteFont font, string text, Vector2 location, Color color)
            => _batch.DrawString(font, text, location, color);
    }
}