using System;
using Thundershock.Core;

namespace Thundershock
{
    public struct HslColor
    {
        /// <summary>
        /// The color's hue.
        /// </summary>
        public float Hue;
        
        /// <summary>
        /// The color's saturation.
        /// </summary>
        public float Saturation;
        
        /// <summary>
        /// The color's luminance.
        /// </summary>
        public float Luminance;
        
        /// <summary>
        /// The translucency or alpha channel of the color.
        /// </summary>
        public float Alpha;
        
        /// <summary>
        /// Creates a new instance of the <see cref="HslColor"/> structure.
        /// </summary>
        /// <param name="h">The desired color's hue.</param>
        /// <param name="s">The desired color's saturation.</param>
        /// <param name="l">The desired color's luminance.</param>
        /// <param name="a">The desired color's alpha channel.</param>
        public HslColor(float h, float s, float l, float a)
        {
            Hue = h;
            Saturation = s;
            Luminance = l;
            Alpha = a;
        }
        
        /// <summary>
        /// Creates a new instance of the <see cref="HslColor"/> structure.
        /// </summary>
        /// <param name="h">The desired color's hue.</param>
        /// <param name="s">The desired color's saturation.</param>
        /// <param name="l">The desired color's luminance.</param>
        public HslColor(float h, float s, float l) : this(h, s, l, 1) {}

        /// <summary>
        /// Computes the HSL values of the <paramref name="color"/> given in the right operand and returns the result
        /// as an <see cref="HslColor"/>.
        /// </summary>
        /// <param name="color">The color whose HSL values need to be computed.</param>
        /// <returns>The computed HSL value of the color.</returns>
        public static implicit operator HslColor(Color color)
        {
            return HslFromRgb(color);
        }

        /// <summary>
        /// Computes the RGBA values of the given <see cref="HslColor"/> specified in <paramref name="hsl"/>.
        /// </summary>
        /// <param name="hsl">The HSL color value to be computed.</param>
        /// <returns>A color structure representing the computed RGBA color values.</returns>
        public static Color HslToColor(HslColor hsl)
        {
            return HslToColor(hsl.Hue, hsl.Saturation, hsl.Luminance, hsl.Alpha);
        }

        /// <summary>
        /// Computes the RGBA color of the given HSL values and alpha channel.
        /// </summary>
        /// <param name="hue">The hue of the desired color.</param>
        /// <param name="saturation">The saturation of the desired color.</param>
        /// <param name="luminance">The luminance of the desired color.</param>
        /// <param name="alpha">The alpha channel, or translucency, of the desired color.</param>
        /// <returns>A color structure representing the computed RGBA values.</returns>
        public static Color HslToColor(float hue, float saturation, float luminance, float alpha = 1)
        {
            var color = new Color();

            color.A = MathHelper.Clamp(alpha, 0, 1);

            if (Math.Abs(saturation) < .001f)
            {
                color.R = MathHelper.Clamp(luminance, 0, 1);
                color.G = color.R;
                color.B = color.G;
            }
            else
            {
                var v2 = (luminance + saturation) - (saturation * luminance);

                if (luminance < 0.5f)
                {
                    v2 = luminance * (1 + saturation);
                }

                var v1 = 2f * luminance - v2;

                color.R = HueToRgb(v1, v2, hue + (1f / 3f));
                color.G = HueToRgb(v1, v2, hue);
                color.B = HueToRgb(v1, v2, hue - (1f / 3f));
            }


            return color;
        }
        
        /// <summary>
        /// Computes the HSL values of the <paramref name="color"/> and returns the result
        /// as an <see cref="HslColor"/>.
        /// </summary>
        /// <param name="color">The color whose HSL values need to be computed.</param>
        /// <returns>The computed HSL value of the color.</returns>
        public static HslColor HslFromRgb(Color color)
        {
            var hsl = new HslColor(0, 0, 0);

            var r = color.R;
            var g = color.G;
            var b = color.B;
            var a = color.A;

            hsl.Alpha = a;

            var min = Math.Min(Math.Min(r, g), b);
            var max = Math.Max(Math.Max(r, g), b);

            var chroma = max - min;

            hsl.Luminance = (max + min) / 2f;

            if (chroma > 0)
            {
                if (hsl.Luminance < 0.5f)
                {
                    hsl.Saturation = chroma / (max + min);
                }
                else
                {
                    hsl.Saturation = chroma / (2 - max - min);
                }
                
                var deltaR = (((max - r) / 6f) + (chroma / 2f)) / chroma;
                var deltaG = (((max - g) / 6f) + (chroma / 2f)) / chroma;
                var deltaB = (((max - b) / 6f) + (chroma / 2f)) / chroma;

                if (Math.Abs(r - max) < 0.001f)
                {
                    hsl.Hue = deltaB - deltaG;
                }
                else if (Math.Abs(g - max) < 0.001f)
                {
                    hsl.Hue = (1f / 3f) + deltaR - deltaB;
                }
                else if (Math.Abs(b - max) < 0.001f)
                {
                    hsl.Hue = (2f / 3f) + deltaG - deltaR;
                }

                if (hsl.Hue < 0)
                    hsl.Hue += 1;
                else if (hsl.Hue > 1)
                    hsl.Hue -= 1;
            }
            
            return hsl;
        }

        private static float HueToRgb(float v1, float v2, float vH)
        {
            vH += (vH < 0) ? 1 : 0;
            vH -= (vH > 1) ? 1 : 0;

            var ret = v1;

            if ((6 * vH) < 1)
            {
                ret = (v1 + (v2 - v1) * 6 * vH);
            }
            else if ((2 * vH) < 1)
            {
                ret = v2;
            }
            else if ((3 * vH) < 2)
            {
                ret = (v1 + (v2 - v1) * ((2f / 3f) - vH) * 6f);
            }
            
            return MathHelper.Clamp(ret, 0, 1);
        }
    }
}