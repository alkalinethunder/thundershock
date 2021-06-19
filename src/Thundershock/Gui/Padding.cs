using System.Numerics;

namespace Thundershock.Gui
{
    public struct Padding
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Left + Right;
        public int Height => Top + Bottom;

        public Vector2 Size => new Vector2(Width, Height);
        
        public Padding(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public Padding(int h, int v) : this(h, v, h, v)
        {
        }

        public Padding(int all) : this(all, all)
        {
        }

        public static implicit operator Padding(int all)
            => new Padding(all);
    }
}