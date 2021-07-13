using System;

namespace Thundershock.Core
{
    public static class MathHelper
    {
        public static bool FloatEquality(float a, float b, float tolerance = 0.000001f)
        {
            return Math.Abs(a - b) <= tolerance;
        }
        
        public static T Clamp<T>(T value, T min, T max)
            where T : IComparable
        {
            if (value.CompareTo(max) > 0)
                return max;
            if (value.CompareTo(min) < 0)
                return min;
            return value;
        }

        public static float ToRadians(float degrees)
        {
            return (MathF.PI / 180) * degrees;
        }
    }
}