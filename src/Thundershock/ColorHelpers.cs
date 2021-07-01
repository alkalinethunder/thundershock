using Thundershock.Core;

namespace Thundershock
{
    public static class ColorHelpers
    {
        public static HslColor ToHsl(this Color color)
        {
            return HslColor.HslFromRgb(color);
        }

        public static Color ToRgb(this HslColor color)
        {
            return HslColor.HslToColor(color);
        }

        public static Color Lighten(this Color color, float percentage)
        {
            var hsl = color.ToHsl();
            var lumDelta = MathHelper.Clamp(hsl.Luminance * percentage, 0, 1);
            hsl.Luminance += lumDelta;
            return hsl.ToRgb();
        }
        
        public static Color Darken(this Color color, float percentage)
        {
            var hsl = color.ToHsl();
            var lumDelta = MathHelper.Clamp(hsl.Luminance * percentage, 0, 1);
            hsl.Luminance -= lumDelta;
            return hsl.ToRgb();
        }
    }
}