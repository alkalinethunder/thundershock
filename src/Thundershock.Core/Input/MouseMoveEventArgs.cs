namespace Thundershock.Core.Input
{
    public sealed class MouseMoveEventArgs : MouseEventArgs
    {
        public int DeltaX { get; }
        public int DeltaY { get; }

        public int LastX => X - DeltaX;
        public int LastY => Y - DeltaY;
        
        public MouseMoveEventArgs(int x, int y, int xDelta, int yDelta) : base(x, y)
        {
            DeltaX = xDelta;
            DeltaY = yDelta;
        }
    }
}