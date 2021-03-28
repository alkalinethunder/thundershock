using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Thundershock
{
    public abstract class App
    {
        private TimeSpan _uptime;
        private TimeSpan _frametime;

        public int ScreenWidth => 0;
        public int ScreenHeight => 0;
        public TimeSpan UpTime => _uptime;
        public TimeSpan FrameTime => _frametime;
        public GameWindow Window => null;
        public ContentManager Content => null;

        public T GetComponent<T>() where T : GlobalComponent, new()
        {
            throw new NotImplementedException();
        }
    }
}