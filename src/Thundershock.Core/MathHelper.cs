using System;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using Microsoft.VisualBasic.CompilerServices;

namespace Thundershock.Core
{
    public static class MathHelper
    {
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