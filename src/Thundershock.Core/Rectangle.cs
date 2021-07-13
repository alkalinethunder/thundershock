using System;
using System.Numerics;

namespace Thundershock.Core
{
    public struct Rectangle
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public Rectangle(float x, float y, float w, float h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
        }

        public float Left => X;
        public float Top => Y;
        public float Right => X + Width;
        public float Bottom => Y + Height;

        public Vector2 Location => new Vector2(X, Y);
        public Vector2 Center => new Vector2(X + (Width / 2), Y + (Height / 2));
        public Vector2 Size => new Vector2(Width, Height);
        public bool IsEmpty => Width * Height == 0;

        public bool IntersectsWith(Rectangle rect)
        {
            if (rect.Bottom < Top)
                return false;

            if (rect.Right < Left)
                return false;

            if (rect.Left > Right)
                return false;

            if (rect.Top > Bottom)
                return false;

            return true;
        }
        
        public static Rectangle Unit => new Rectangle(0, 0, 1, 1);
        public static Rectangle Empty => new Rectangle(0, 0, 0, 0);
        
        public static bool operator ==(Rectangle a, Rectangle b)
        {
            return MathHelper.FloatEquality(a.X, b.X)
                   && MathHelper.FloatEquality(a.Y, b.Y)
                   && MathHelper.FloatEquality(a.Width, b.Width)
                   && MathHelper.FloatEquality(a.Height, b.Height);
        }

        public static bool operator !=(Rectangle a, Rectangle b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj is Rectangle rect && rect == this;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Location, Size);
        }

        public override string ToString()
        {
            return $"(X = {X}, Y = {Y}, Width = {Width}, Height = {Height})";
        }

        public static Rectangle Intersect(Rectangle a, Rectangle b)
        {
            if (a.IntersectsWith(b))
            {
                var right = Math.Min(a.Right, b.Right);
                var left = Math.Max(a.Left, b.Left);
                var top = Math.Max(a.Top, b.Top);
                var bottom = Math.Min(a.Bottom, b.Bottom);

                return new Rectangle(left, top, right - left, bottom - top);
            }

            return Empty;
        }
    }
}