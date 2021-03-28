using System;
using Microsoft.Xna.Framework.Input;

namespace Thundershock.Input
{
    public class MouseEventArgs : EventArgs
    {
        public int XPosition { get; }
        public int YPosition { get; }
        
        public int WheelValue { get; }
        public int HorizWheelValue { get; }
        
        public ButtonState PrimaryState { get; }
        public ButtonState SecondaryState { get; }
        public ButtonState MiddleState { get; }
        public ButtonState X1State { get; }
        public ButtonState X2State { get; }

        public MouseEventArgs(MouseState mouseState)
        {
            XPosition = mouseState.Position.X;
            YPosition = mouseState.Position.Y;

            WheelValue = mouseState.ScrollWheelValue;
            HorizWheelValue = mouseState.HorizontalScrollWheelValue;

            PrimaryState = mouseState.LeftButton;
            SecondaryState = mouseState.RightButton;
            MiddleState = mouseState.MiddleButton;
            X1State = mouseState.XButton1;
            X2State = mouseState.XButton2;
        }
    }
}