using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Gui.Styling
{
    public class BasicStyle : GuiStyle
    {
        private Color _selectionColor = Color.Blue;
        
        public override SpriteFont DefaultFont => Gui.FallbackFont;
        
        public override void DrawSelectionBox(GuiRenderer renderer, Rectangle bounds, SelectionStyle selectionStyle)
        {
            var color = _selectionColor;
            if (selectionStyle == SelectionStyle.ItemHover)
                color *= 0.67f;

            renderer.FillRectangle(bounds, color);
        }
    }
}