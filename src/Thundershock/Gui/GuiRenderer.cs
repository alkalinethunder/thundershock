using System.Numerics;
using Gdk;
using Thundershock.Core;
using Thundershock.Core.Rendering;
using Color = Thundershock.Core.Color;
using Rectangle = Thundershock.Core.Rectangle;

namespace Thundershock.Gui
{
    public class GuiRenderer
    {
        private Renderer2D _spriteBatch;
        private GuiSystem.GuiRendererState _state;

        public float Layer => _spriteBatch.Z;
        
        public void ComputeColor(ref Color color)
        {
            color *= _state.Tint * _state.Opacity;
        }
        
        internal GuiRenderer(Renderer2D batcher, GuiSystem.GuiRendererState state)
        {
            _spriteBatch = batcher;
            _state = state;
        }

        public void FillRectangle(Rectangle rect, Texture2D texture, Color color, Rectangle uv)
        {
            ComputeColor(ref color);

            _spriteBatch.FillRectangle(rect, color, texture, uv);
        }
        
        public void FillRectangle(Rectangle rect, Color color)
        {
            ComputeColor(ref color);
            
            _spriteBatch.FillRectangle(rect, color);
        }

        public void FillRectangle(Rectangle rect, Texture2D texture, Color color)
        {
            ComputeColor(ref color);
            
            _spriteBatch.FillRectangle(rect, color, texture);
        }
        
        public void DrawRectangle(Rectangle rect, Color color, int thickness)
        {
            ComputeColor(ref color);
            
            _spriteBatch.DrawRectangle(rect, color, thickness);
        }

        public void DrawString(Font font, string text, Vector2 position, Color color)
        {
            ComputeColor(ref color);
            
            if (color.A <= 0)
                return;

            _spriteBatch.DrawString(font, text, position, color);
        }

        public void DrawString(Font font, string text, Vector2 position, Color color,
            int dropShadow)
            => DrawString(font, text, position, color, dropShadow, Color.Black);

        
        public void DrawString(Font font, string text, Vector2 position, Color color,
            int dropShadow, Color dropShadowColor)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            
            if (dropShadow != 0)
            {
                DrawString(font, text, position + new Vector2(dropShadow, dropShadow), dropShadowColor);
            }

            DrawString(font, text, position, color);
        }

        public void FillCircle(Vector2 center, float radius, Color color)
            => FillCircle(center, radius, null, color);
        
        public void FillCircle(Vector2 center, float radius, Texture2D texture, Color tint)
        {
            ComputeColor(ref tint);
            if (tint.A <= 0)
                return;
            
            _spriteBatch.FillCircle(center, radius, tint, texture);
        }

        public void DrawText(TextRenderBuffer text)
        {
            _spriteBatch.DrawText(text);
        }
    }
}