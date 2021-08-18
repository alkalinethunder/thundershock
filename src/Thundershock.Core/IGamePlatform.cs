using System;
using System.Collections.Generic;
using Thundershock.Core.Audio;
using Thundershock.Core.Rendering;

namespace Thundershock.Core
{
    public interface IGamePlatform
    {
        int GetMonitorCount();
        DisplayMode GetDefaultDisplayMode(int monitor);
        IEnumerable<DisplayMode> GetAvailableDisplayModes(int monitor);
        string GraphicsCardDescription { get; }

        GraphicsProcessor CreateGraphicsProcessor();
    }
}