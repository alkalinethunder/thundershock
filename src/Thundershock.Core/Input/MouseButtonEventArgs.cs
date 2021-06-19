namespace Thundershock.Core.Input
{
    public sealed class MouseButtonEventArgs : MouseEventArgs
    {
        public MouseButton Button { get; }
        public ButtonState ButtonState { get; }

        public bool IsPressed => ButtonState == ButtonState.Pressed;

        public MouseButtonEventArgs(int x, int y, MouseButton button, ButtonState state) : base(x, y)
        {
            Button = button;
            ButtonState = state;
        }
    }
}