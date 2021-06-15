using System;

namespace Thundershock.Core
{
    public class GameTime
    {
        public TimeSpan ElapsedGameTime { get; }
        public TimeSpan TotalGameTime { get; }

        public GameTime(TimeSpan elapsed, TimeSpan total)
        {
            ElapsedGameTime = elapsed;
            TotalGameTime = total;
        }
    }
}