using System.Collections.Generic;

namespace Thundershock.Core
{
    public interface IGamePlatform
    {
        int GetMonitorCount();
        DisplayMode GetDefaultDisplayMode(int monitor);
        IEnumerable<DisplayMode> GetAvailableDisplayModes(int monitor);
        string GraphicsCardDescription { get; }
    }
}