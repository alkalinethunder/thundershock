using Thundershock.Core;
using Thundershock.Gui.Elements;
using System.Numerics;

namespace Thundershock.Gui.Styling
{
    public class BasicStyle : GuiStyle
    {
        private Color _bgColor = Color.White;
        private Color _selectionColor = Color.Blue;
        private Color _buttonColor = ThundershockPlatform.HtmlColor("#343434");
        private Color _activeButtonColor = ThundershockPlatform.HtmlColor("#1baaf7");
        
        public override Font DefaultFont => Gui.FallbackFont;
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

        public override void DrawStringListBackground(GuiRenderer renderer, StringList stringList)
        {
            renderer.FillRectangle(stringList.BoundingBox,
                (stringList.BackColor ?? StyleColor.Default).GetColor(Color.White));
        }

        public override void DrawListItem(GuiRenderer renderer, StringList stringList, Rectangle bounds, bool isActive, bool isHovered,
            string text)
        {
            if (isActive)
                DrawSelectionBox(renderer, bounds, SelectionStyle.ItemActive);
            else if (isHovered)
                DrawSelectionBox(renderer, bounds, SelectionStyle.ItemHover);

            var color = ((isActive ? stringList.ItemsActiveColor : stringList.ItemsColor) ?? StyleColor.Default)
                .GetColor(isActive ? Color.White : Color.Black);
            var font = stringList.Font.GetFont(GetFont(stringList));
            
            renderer.DrawString(font, text, bounds.Location, color);
        }

        public override void PaintElementBackground(Element element, GameTime gameTime, GuiRenderer renderer)
        {
            renderer.FillRectangle(element.BoundingBox, _bgColor);
        }

        public override void PaintMenuItemText(Element element, GameTime gameTime, GuiRenderer renderer, string text, Font font,
            Vector2 textPos, SelectionStyle selectionStyle)
        {
            var color = (selectionStyle == SelectionStyle.None) ? Color.Black : Color.White;

            renderer.DrawString(font, text, textPos, color);
        }
    }
}