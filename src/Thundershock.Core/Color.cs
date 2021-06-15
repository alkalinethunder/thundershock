using System;
using System.Numerics;

namespace Thundershock.Core
{
    public struct Color
    {
        public byte R;
        public byte G;
        public byte B;
        public byte Alpha;

        public Color(byte r, byte g, byte b, byte alpha = 255)
        {
            R = r;
            G = g;
            B = b;
            Alpha = alpha;
        }
        
        public static implicit operator Color(System.Drawing.Color gdiColor)
        {
            return new Color(gdiColor.R, gdiColor.G, gdiColor.B, gdiColor.A);
        }

        public static Color Transparent => new(0, 0, 0, 0);

        public static Color operator *(Color a, float b)
        {
            return new Color(a.R, a.G, a.B, (byte) (a.Alpha * b));
        }

        public static bool operator ==(Color a, Color b)
        {
            return a.R == b.R && a.G == b.G && a.B == b.B && a.Alpha == b.Alpha;
        }

        public static bool operator !=(Color a, Color b)
        {
            return !(a == b);
        }

        public override bool Equals(object? obj)
        {
            return obj is Color c && c == this;
        }

        public override string ToString()
        {
            return $"(R={R},G={G},B={B}, alpha={Alpha})";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B, Alpha);
        }

        public static Color Black => new Color(0, 0, 0);
        
        public Vector4 ToVector4()
        {
            return new Vector4(
                (float) R / 255,
                (float) G / 255,
                (float) B / 255,
                (float) Alpha / 255
            );
        }
    }
}