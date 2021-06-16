using System.Drawing;

namespace Thundershock.Core
{
    public class DisplayMode
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Monitor { get; }
        public int MonitorX { get; }
        public int MonitorY { get; }
        
        public Rectangle Bounds => new Rectangle(MonitorX, MonitorY, Width, Height);

        public DisplayMode(int width, int height, int monitor, int x, int y)
        {
            Width = width;
            Height = height;
            Monitor = Monitor;
            MonitorX = x;
            MonitorY = y;
        }
    }
}