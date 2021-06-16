using System;

namespace Thundershock.Core.Input
{
    public abstract class MouseEventArgs : EventArgs
    {
        public int X { get; }
        public int Y { get; }

        public MouseEventArgs(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

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