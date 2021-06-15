using Microsoft.Xna.Framework;
using System;
using Microsoft.Xna.Framework.Input;

namespace Thundershock.Input
{
    public class InputManager : GameAppComponent
    {
        private MouseEventArgs _lastMouseEvent;

        public event EventHandler<MouseMoveEventArgs> MouseMove;
        public event EventHandler<MouseScrollEventArgs> MouseScroll;
        public event EventHandler<MouseScrollEventArgs> MouseHorizontalScroll;
        public event EventHandler<MouseButtonEventArgs> MouseUp;
        public event EventHandler<MouseButtonEventArgs> MouseDown;
        public event EventHandler<KeyEventArgs> KeyDown;
        public event EventHandler<KeyEventArgs> KeyUp;
        public event EventHandler<KeyCharEventArgs> KeyChar;

        public bool EnableInput { get; set; } = true;
        
        
        protected override void OnLoad()
        {
            base.OnLoad();
            _lastMouseEvent = new MouseEventArgs(Mouse.GetState());
            
            App.Window.KeyDown += ForwardKeyDown;
            App.Window.KeyUp += ForwardKeyUp;
            App.Window.TextInput += ForwardText;
        }

        private void ForwardText(object sender, TextInputEventArgs e)
        {
            if (EnableInput)
                KeyChar?.Invoke(this, new KeyCharEventArgs(e.Key, e.Character));
        }

        private void ForwardKeyUp(object sender, InputKeyEventArgs e)
        {
            if (EnableInput)
                KeyUp?.Invoke(this, new KeyEventArgs(e.Key));
        }

        private void ForwardKeyDown(object sender, InputKeyEventArgs e)
        {
            if (EnableInput)
                KeyDown?.Invoke(this, new KeyEventArgs(e.Key));
        }

        private void ProcessButton(MouseState mouseState, MouseButton button, ButtonState prev, ButtonState curr)
        {
            if (prev != curr)
            {
                var buttonEvent = new MouseButtonEventArgs(mouseState, button, curr);
                if (prev == ButtonState.Pressed)
                {
                    MouseUp?.Invoke(this, buttonEvent);
                }
                else
                {
                    MouseDown?.Invoke(this, buttonEvent);
                }
            }
        }

        protected override void OnUpdate(Thundershock.Core.GameTime gameTime)
        {
            base.OnUpdate(gameTime);

            if (EnableInput)
            {
                var mouseState = Mouse.GetState();

                var x = mouseState.X;
                var y = mouseState.Y;

                var xDelta = x - _lastMouseEvent.XPosition;
                var yDelta = y - _lastMouseEvent.YPosition;

                if (xDelta != 0 || yDelta != 0)
                {
                    var moveEvent = new MouseMoveEventArgs(mouseState, xDelta, yDelta);
                    MouseMove?.Invoke(this, moveEvent);
                }

                var vWheel = mouseState.ScrollWheelValue;
                var hWheel = mouseState.HorizontalScrollWheelValue;

                var vDelta = vWheel - _lastMouseEvent.WheelValue;
                var hDelta = hWheel - _lastMouseEvent.HorizWheelValue;

                if (vDelta != 0)
                {
                    var scrollEvent = new MouseScrollEventArgs(mouseState, ScrollDirection.Vertical, vDelta);
                    MouseScroll?.Invoke(this, scrollEvent);
                }

                if (hDelta != 0)
                {
                    var scrollEvent = new MouseScrollEventArgs(mouseState, ScrollDirection.Horizontal, hDelta);
                    MouseHorizontalScroll?.Invoke(this, scrollEvent);
                }

                ProcessButton(mouseState, MouseButton.Primary, _lastMouseEvent.PrimaryState, mouseState.LeftButton);
                ProcessButton(mouseState, MouseButton.Secondary, _lastMouseEvent.SecondaryState,
                    mouseState.RightButton);
                ProcessButton(mouseState, MouseButton.Middle, _lastMouseEvent.MiddleState, mouseState.MiddleButton);
                ProcessButton(mouseState, MouseButton.XButton1, _lastMouseEvent.X1State, mouseState.XButton1);
                ProcessButton(mouseState, MouseButton.XButton2, _lastMouseEvent.X2State, mouseState.XButton2);

                _lastMouseEvent = new MouseEventArgs(mouseState);
            }
        }
    }
}