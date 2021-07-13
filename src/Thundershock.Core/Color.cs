using System;
using System.Numerics;

namespace Thundershock.Core
{
    public struct Color
    {
        public float R;
        public float G;
        public float B;
        public float A;

        private float Alpha => A;
        private float RValue => R;
        private float GValue => G;
        private float BValue => B;
        
        public Color(float r, float g, float b, float alpha = 1)
        {
            R = r;
            G = g;
            B = b;
            A = alpha;
        }

        public Color(Color color, float alpha)
        {
            R = color.R;
            G = color.G;
            B = color.B;
            A = alpha;
        }
        
        public Color(int r, int g, int b, int a = 0xff) : this(r / 255f, g / 255f, b / 255f, a / 255f) {}

        public static implicit operator Color(System.Drawing.Color gdiColor)
        {
            return new(gdiColor.R / 255f, gdiColor.G / 255f, gdiColor.B / 255f, gdiColor.A / 255f);
        }

        public static readonly Color Cyan = new Color(0, 0xff, 0xff);
        public static readonly Color Red = new Color(0xff, 0, 0);
        public static readonly Color Green = new Color(0, 0xff, 0);
        public static readonly Color Magenta = new Color(0xff, 0, 0xff);
        public static readonly Color Yellow = new Color(0xff, 0xff, 0);
        public static readonly Color Orange = new Color(0x80, 0x80, 0);
        public static readonly Color DarkGray = new Color(0x64, 0x64, 0x64);
        public static readonly Color DarkRed = new Color(0x80, 0, 0);
        public static readonly Color DarkGreen = new Color(0, 0x80, 0);
        public static readonly Color DarkBlue = new Color(0, 0, 0x80);
        public static readonly Color DarkCyan = new Color(0, 0x80, 0x80);
        public static readonly Color DarkMagenta = new Color(0x80, 0, 0x80);
        public static readonly Color Black = new Color(0, 0, 0);
        public static readonly Color White = new Color(0xff, 0xff, 0xff);
        public static readonly Color Blue = new Color(0, 0, 0xff);
        public static readonly Color Transparent = new(0, 0, 0, 0);
        public static readonly Color Gray = new Color(0x80, 0x80, 0x80);
        
        public static Color operator *(Color a, float b)
        {
            return new Color(a.R, a.G, a.B, (a.A * b));
        }

        public static bool operator ==(Color a, Color b)
        {
            return MathHelper.FloatEquality(a.R, b.R)
                   && MathHelper.FloatEquality(a.G, b.G)
                   && MathHelper.FloatEquality(a.B, b.B)
                   && MathHelper.FloatEquality(a.A, b.A);

        }

        public static bool operator !=(Color a, Color b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj is Color c && c == this;
        }

        public override string ToString()
        {
            return $"(R={R},G={G},B={B}, alpha={Alpha})";
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(RValue, GValue, BValue, Alpha);
        }

        
        private Vector4 ToVector4()
        {
            return new Vector4(
                R,
                G,
                B,
                A
            );
        }
        
        public static Color Lerp(Color a, Color b, float value)
        {
            var aVector = a.ToVector4();
            var bVector = b.ToVector4();

            var c = Vector4.Lerp(aVector, bVector, value);

            return new Color(c.X, c.Y, c.Z, c.W);
        }

        public static implicit operator System.Drawing.Color(Color color)
        {
            return System.Drawing.Color.FromArgb((byte) (color.A * 255), (byte) (color.R * 255), (byte) (color.G * 255),
                (byte) (color.B * 255));
        }
    }
}