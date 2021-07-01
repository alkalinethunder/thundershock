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
}