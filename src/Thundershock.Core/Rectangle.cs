using System;
using System.Numerics;

namespace Thundershock.Core
{
    public struct Rectangle
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Rectangle(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
        }

        public int Left => X;
        public int Top => Y;
        public int Right => X + Width;
        public int Bottom => Y + Height;

        public Vector2 Location => new Vector2(X, Y);
        public Vector2 Center => new Vector2(X + (Width / 2), Y + (Height / 2));
        public bool IsEmpty => Width * Height == 0;
        
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