using Microsoft.Xna.Framework.Input;

namespace Thundershock.Input
{
    public class MouseScrollEventArgs : MouseEventArgs
    {
        public ScrollDirection Direction { get; }
        public int WheelDelta { get; }

        public int Previous => ((Direction == ScrollDirection.Horizontal) ? HorizWheelValue : WheelValue) - WheelDelta;

        public MouseScrollEventArgs(MouseState mouseState, ScrollDirection direction, int delta) : base(mouseState)
        {
            Direction = direction;
            WheelDelta = delta;
        }
    }
}