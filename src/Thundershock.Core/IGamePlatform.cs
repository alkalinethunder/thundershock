using System.Collections.Generic;
using Thundershock.Core.Audio;

namespace Thundershock.Core
{
    public interface IGamePlatform
    {
        AudioBackend Audio { get; }
        int GetMonitorCount();
        DisplayMode GetDefaultDisplayMode(int monitor);
        IEnumerable<DisplayMode> GetAvailableDisplayModes(int monitor);
        string GraphicsCardDescription { get; }
    }
}