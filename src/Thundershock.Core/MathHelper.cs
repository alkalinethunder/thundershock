using System.Numerics;

namespace Thundershock.Core
{
    public static class MathHelper
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;
            return value;
        }
    }
}