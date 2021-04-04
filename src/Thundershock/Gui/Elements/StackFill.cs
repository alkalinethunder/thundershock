using Microsoft.Xna.Framework;

namespace Thundershock.Gui.Elements
{
    public struct StackFill
    {
        public float Percentage;

        public StackFill(float percentage)
        {
            Percentage = MathHelper.Clamp(percentage, 0, 1);
        }

        public static StackFill Auto
            => new StackFill(0);

        public static StackFill Fill
            => new StackFill(1);
        
        public static implicit operator StackFill(float percentage)
            => new StackFill(percentage);
    }
}