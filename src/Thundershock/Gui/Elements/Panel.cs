using Thundershock.Core;

namespace Thundershock.Gui.Elements
{
    public class Panel : ContentElement
    {
        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            GuiSystem.Style.PaintElementBackground(this, gameTime, renderer);
        }
    }
}