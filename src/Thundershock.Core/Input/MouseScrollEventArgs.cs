namespace Thundershock.Core.Input
{
    public sealed class MouseScrollEventArgs : MouseEventArgs
    {
        public int WheelValue { get; }
        public int WheelDelta { get; }
        public ScrollDirection ScrollDirection { get; }
        public int LastWheelValue => WheelValue - WheelDelta;

        public MouseScrollEventArgs(int x, int y, int wheel, int delta, ScrollDirection direction) : base(x, y)
        {
            WheelValue = wheel;
            WheelDelta = delta;
            ScrollDirection = direction;
        }
    }
}