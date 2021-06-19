using Thundershock.Core;

namespace Thundershock.Gui.Elements
{
    public class Panel : Element
    {
        public Color BackColor { get; set; } = Color.White;

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            renderer.FillRectangle(BoundingBox, BackColor);
        }
    }
}