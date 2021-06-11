using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Gui.Elements;

namespace Thundershock.Gui.Styling
{
    public class BasicStyle : GuiStyle
    {
        private Color _selectionColor = Color.Blue;
        private Color _buttonColor = ThundershockPlatform.HtmlColor("#343434");
        private Color _activeButtonColor = ThundershockPlatform.HtmlColor("#1baaf7");
        
        public override SpriteFont DefaultFont => Gui.FallbackFont;
        public override int CheckSize => 18;
        public override int TextCursorWidth => 1;
        
        public override void DrawSelectionBox(GuiRenderer renderer, Rectangle bounds, SelectionStyle selectionStyle)
        {
            var color = _selectionColor;
            if (selectionStyle == SelectionStyle.ItemHover)
                color *= 0.67f;

            renderer.FillRectangle(bounds, color);
        }

        public override void DrawCheckBox(GuiRenderer renderer, CheckBox checkBox, Rectangle bounds)
        {
            var color = (checkBox.CheckColor ?? StyleColor.Default).GetColor(Color.Gray);

            if (checkBox.IsHovered)
                renderer.FillRectangle(bounds, color * 0.5f);
            
            renderer.DrawRectangle(bounds, color, 2);

            if (checkBox.IsChecked)
            {
                bounds.X += 4;
                bounds.Y += 4;
                bounds.Width -= 8;
                bounds.Height -= 8;
                renderer.FillRectangle(bounds, color);
            }
        }

        public override void DrawTextCursor(GuiRenderer renderer, Color color, Vector2 position, int height)
        {
            var rect = new Rectangle((int) position.X, (int) position.Y, TextCursorWidth, height);

            renderer.FillRectangle(rect, color);
        }

        public override void DrawButton(GuiRenderer renderer, IButtonElement button)
        {
            var styleColor = button.ButtonColor ?? StyleColor.Default;
            var color = styleColor.GetColor(button.IsActive ? _activeButtonColor : _buttonColor);

            if (button.IsPressed)
                color = color.Darken(0.3f);
            else if (button.IsHovered)
                color = color.Lighten(0.2f);

            var borderColor = color.Lighten(0.15f);

            renderer.FillRectangle(button.BoundingBox, color);
            renderer.DrawRectangle(button.BoundingBox, borderColor, 2);
        }

        public override Color GetButtonTextColor(IButtonElement button)
        {
            var styleColor = button.ButtonColor ?? StyleColor.Default;
            var color = styleColor.GetColor(button.IsActive ? _activeButtonColor : _buttonColor);

            if (button.IsPressed)
                color = color.Darken(0.3f);
            else if (button.IsHovered)
                color = color.Lighten(0.2f);

            var brightness = color.ToHsl().Luminance;

            if (brightness > 0.5f)
                return Color.Black;
            else
                return Color.White;
        }
    }
}