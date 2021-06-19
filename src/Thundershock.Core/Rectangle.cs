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
        public bool IsEmpty => Width * Height == 0;

        public static Rectangle Unit => new Rectangle(0, 0, 1, 1);
        public static Rectangle Empty => new Rectangle(0, 0, 0, 0);
        
        public static bool operator ==(Rectangle a, Rectangle b)
        {
            return (a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height);
        }

        public static bool operator !=(Rectangle a, Rectangle b)
        {
            return !(a == b);
        }

        public override bool Equals(object? obj)
        {
            return obj is Rectangle rect && rect == this;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        public override string ToString()
        {
            return $"(X = {X}, Y = {Y}, Width = {Width}, Height = {Height})";
        }
    }
}