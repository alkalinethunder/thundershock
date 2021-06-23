using System;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock.Gui
{
    public class GuiRenderer
    {
        private float _opacity;
        private Color _masterTint;
        private Renderer2D _spriteBatch;
        private Rectangle _clipRect;
        
        private Color GetProperTint(Color tint)
        {
            var r = tint.R;
            var g = tint.G;
            var b = tint.B;

            var mr = _masterTint.R;
            var mg = _masterTint.G;
            var mb = _masterTint.B;

            r *= mr;
            g *= mg;
            b *= mb;

            return new Color(r, g, b, tint.A) * _opacity;
        }
        
        public GuiRenderer(Renderer2D batch, float opacity, Color tint)
        {
            _spriteBatch = batch;
            _opacity = opacity;
            _masterTint = tint;
        }
        
        public void FillRectangle(Rectangle rect, Color color)
        {
            _spriteBatch.FillRectangle(rect, GetProperTint(color));
        }

        public void FillRectangle(Rectangle rect, Texture2D texture, Color color)
        {
            _spriteBatch.FillRectangle(rect, GetProperTint(color), texture);
        }
        
        public void DrawRectangle(Rectangle rect, Color color, int thickness)
        {
            _spriteBatch.DrawRectangle(rect, color, thickness);
        }

        public void DrawString(Font font, string text, Vector2 position, Color color,
            int dropShadow = 0)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var tint = GetProperTint(color);
            if (tint.A <= 0)
                return;
            
            if (dropShadow != 0)
            {
                DrawString(font, text, position + new Vector2(dropShadow, dropShadow), Color.Black);
            }

            _spriteBatch.DrawString(font, text, position, tint);
        }
    }
}