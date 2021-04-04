using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Gui.Elements
{
    public class TextBlock : Element
    {
        public Color Color { get; set; } = Color.Black;
        public string Text { get; set; } = "Text Block";
        public SpriteFont Font { get; set; }

        public TextAlign TextAlign { get; set; }
        
        private SpriteFont GetFont()
        {
            return Font ?? GuiSystem.FallbackFont;
        }

        protected override Vector2 MeasureOverride()
        {
            return GetFont().MeasureString(Text);
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            if (!string.IsNullOrWhiteSpace(Text))
            {
                var f = GetFont();

                renderer.DrawString(f, Text, BoundingBox.Location.ToVector2(), Color, TextAlign);
            }
        }
    }
}