using System;

namespace Thundershock.Core
{
    public struct DisplayMode
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Monitor;
        public readonly int MonitorX;
        public readonly int MonitorY;
        
        public Rectangle Bounds => new Rectangle(MonitorX, MonitorY, Width, Height);

        public DisplayMode(int width, int height, int monitor, int x, int y)
        {
            Width = width;
            Height = height;
            Monitor = monitor;
            MonitorX = x;
            MonitorY = y;
        }

        public bool IsInvalid
            => (Width * Height <= 0) || (Monitor < 0) || (MonitorX < 0) || (MonitorY < 0);
        
        public static readonly DisplayMode Invalid = new (0, 0, -1, -1, -1);
        
        public static bool operator ==(DisplayMode a, DisplayMode b)
        {
            return (a.Width == b.Width && a.Height == b.Height && a.Monitor == b.Monitor && a.MonitorX == b.MonitorX &&
                    a.MonitorY == b.MonitorY);
        }

        public static bool operator !=(DisplayMode a, DisplayMode b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj is DisplayMode mode && mode == this;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height, Monitor, MonitorX, MonitorY);
        }

        public override string ToString()
        {
            return $"{Width}x{Height} (Display {Monitor}, x={MonitorX} y={MonitorY})";
        }
    }
}