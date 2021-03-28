using Microsoft.Xna.Framework.Input;

namespace Thundershock.Input
{
    public class MouseMoveEventArgs : MouseEventArgs
    {
        public int XDelta { get; }
        public int YDelta { get; }

        public int XPrevious => XPosition - XDelta;
        public int YPrevious => YPosition - YDelta;

        public MouseMoveEventArgs(MouseState mouseState, int xDelta, int yDelta) : base(mouseState)
        {
            XDelta = xDelta;
            YDelta = yDelta;
        } 
    }
}