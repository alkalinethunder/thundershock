using System;
using Thundershock.Core.Input;

namespace Thundershock.Input
{
    public sealed class InputSystem
    {
        public event EventHandler<MouseButtonEventArgs> MouseDown;
        public event EventHandler<MouseButtonEventArgs> MouseUp;
        public event EventHandler<KeyEventArgs> KeyDown;
        public event EventHandler<KeyEventArgs> KeyUp;
        public event EventHandler<KeyCharEventArgs> KeyChar;
        public event EventHandler<MouseMoveEventArgs> MouseMove;
        public event EventHandler<MouseScrollEventArgs> MouseScroll;
        
        public void FireMouseDown(MouseButtonEventArgs e)
        {
            MouseDown?.Invoke(this, e);
        }

        public void FireMouseUp(MouseButtonEventArgs e)
        {
            MouseUp?.Invoke(this, e);
        }

        public void FireMouseMove(MouseMoveEventArgs e)
        {
            MouseMove?.Invoke(this, e);
        }

        public void FireMouseScroll(MouseScrollEventArgs e)
        {
            MouseScroll?.Invoke(this, e);
        }
        
        public void FireKeyChar(KeyCharEventArgs e)
        {
            KeyChar?.Invoke(this, e);
        }
        
        public void FireKeyUp(KeyEventArgs e)
        {
            KeyUp?.Invoke(this, e);
        }

        public void FireKeyDown(KeyEventArgs e)
        {
            KeyDown?.Invoke(this, e);
        }
    }
}