using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Gui.Elements;

namespace Thundershock.Gui.Styling
{
    public class BasicStyle : GuiStyle
    {
        private Color _selectionColor = Color.Blue;
        
        public override SpriteFont DefaultFont => Gui.FallbackFont;
        public override int CheckSize => 18;
        
        public override void DrawSelectionBox(GuiRenderer renderer, Rectangle bounds, SelectionStyle selectionStyle)
        {
            var color = _selectionColor;
            if (selectionStyle == SelectionStyle.ItemHover)
                color *= 0.67f;

            renderer.FillRectangle(bounds, color);
        }

        public override void DrawCheckBox(GuiRenderer renderer, Rectangle bounds, CheckState checkState, bool isHovered)
        {
            var color = Color.Black;

            if (isHovered)
                renderer.FillRectangle(bounds, color * 0.5f);
            
            renderer.DrawRectangle(bounds, color, 2);

            if (checkState == CheckState.Checked)
            {
                bounds.X += 4;
                bounds.Y += 4;
                bounds.Width -= 8;
                bounds.Height -= 8;
                renderer.FillRectangle(bounds, color);
            }
        }
    }
}