using System;
using System.Numerics;
using Thundershock.Core;

namespace Thundershock.Gui
{
    public struct Padding
    {
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;

        public float Width => Left + Right;
        public float Height => Top + Bottom;

        public Vector2 Size => new Vector2(Width, Height);

        private float X1 => Left;
        private float X2 => Right;
        private float Y1 => Top;
        private float Y2 => Bottom;
        
        public Padding(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public Padding(float h, float v) : this(h/2, v/2, h/2, v/2)
        {
        }

        public Padding(float all) : this(all, all, all, all)
        {
        }

        public static implicit operator Padding(float all)
            => new Padding(all);

        public static bool operator ==(Padding a, Padding b)
        {
            return MathHelper.FloatEquality(a.Left, b.Left)
                   && MathHelper.FloatEquality(a.Top, b.Top)
                   && MathHelper.FloatEquality(a.Right, b.Right)
                   && MathHelper.FloatEquality(a.Bottom, b.Bottom);
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