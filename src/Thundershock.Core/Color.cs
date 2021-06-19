using System;
using System.Drawing.Printing;
using System.Numerics;

namespace Thundershock.Core
{
    public struct Color
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public byte Alpha => A;
        
        public Color(byte r, byte g, byte b, byte alpha = 255)
        {
            R = r;
            G = g;
            B = b;
            A = alpha;
        }

        public Color(Color color, float alpha)
        {
            this.R = color.R;
            this.G = color.G;
            this.B = color.B;
            this.A = (byte) (255 * alpha);
        }

        public static implicit operator Color(System.Drawing.Color gdiColor)
        {
            return new Color(gdiColor.R, gdiColor.G, gdiColor.B, gdiColor.A);
        }

        public static Color Cyan => new Color(0, 0xff, 0xff);
        public static Color Red => new Color(0xff, 0, 0);
        public static Color Green => new Color(0, 0xff, 0);
        public static Color Magenta => new Color(0xff, 0, 0xff);
        public static Color Yellow => new Color(0xff, 0xff, 0);
        public static Color Orange => new Color(0x80, 0x80, 0);

        public static Color DarkGray => new Color(0x64, 0x64, 0x64);
        public static Color DarkRed => new Color(0x80, 0, 0);
        public static Color DarkGreen => new Color(0, 0x80, 0);
        public static Color DarkBlue => new Color(0, 0, 0x80);
        public static Color DarkCyan => new Color(0, 0x80, 0x80);
        public static Color DarkMagenta => new Color(0x80, 0, 0x80);
        
        public static Color White => new Color(0xff, 0xff, 0xff);
        public static Color Blue => new Color(0, 0, 0xff);
        public static Color Transparent => new(0, 0, 0, 0);
        public static Color Gray => new Color(0x80, 0x80, 0x80);
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
        
        public static Color Lerp(Color a, Color b, float value)
        {
            var aVector = a.ToVector4();
            var bVector = b.ToVector4();

            var c = Vector4.Lerp(aVector, bVector, value);

            return new Color((byte) c.X, (byte) c.Y, (byte) c.Z, (byte) c.W);
        }

    }
}