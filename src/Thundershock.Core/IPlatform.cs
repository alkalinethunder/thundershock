using Thundershock.Core.Debugging;

namespace Thundershock.Core
{
    public interface IPlatform
    {
        void Initialize(Logger logger);

        int GetMonitorCount();
        DisplayMode GetDefaultDisplayMode(int monitor);
        DisplayMode GetDisplayMode(int width, int height, int monitor);
    }
}