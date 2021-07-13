using System;
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

        private int X1 => Left;
        private int X2 => Right;
        private int Y1 => Top;
        private int Y2 => Bottom;
        
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

        public static bool operator ==(Padding a, Padding b)
        {
            return (a.Left == b.Left && a.Top == b.Top && a.Right == b.Right && a.Bottom == b.Bottom);
        }

        public static bool operator !=(Padding a, Padding b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj is Padding b && b == this;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X1, Y1, X2, Y2);
        }

        public override string ToString()
        {
            return $"(Left={Left}, Top={Top}, Right={Right}, Bottom={Bottom})";
        }
    }
}