using System.Numerics;
using Thundershock.Core;

namespace Thundershock.Gui.Elements
{
    public class ProgressBar : ContentElement
    {
        private float _value = 0;

        public float Value
        {
            get => _value;
            set => _value = MathHelper.Clamp(value, 0, 1);
        }

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            var size = GuiSystem.Style.ProgressBarHeight;
            return new Vector2(size, size);
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            GuiSystem.Style.DrawProgressBar(renderer, this);
        }
    }
}