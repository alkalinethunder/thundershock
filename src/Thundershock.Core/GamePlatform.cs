using System.Collections.Generic;
using System.Linq;
using Thundershock.Core.Audio;
using Thundershock.Core.Debugging;
using Thundershock.Debugging;

namespace Thundershock.Core
{
    [CheatAlias("Sys")]
    public static class GamePlatform
    {
        private static IGamePlatform _gamePlatform;
        
        public static AudioBackend Audio => _gamePlatform.Audio;
        public static int MonitorCount => _gamePlatform.GetMonitorCount();

        public static DisplayMode GetDefaultDisplayMode(int monitor)
        {
            return _gamePlatform.GetDefaultDisplayMode(monitor);
        }
        
        public static string GraphicsCardDescription => _gamePlatform.GraphicsCardDescription;

        public static IEnumerable<DisplayMode> GetAvailableDisplayModes(int monitor)
        {
            return _gamePlatform.GetAvailableDisplayModes(monitor).Distinct();
        }
        public static void Initialize(IGamePlatform gamePlatform)
        {
            _gamePlatform = gamePlatform;
        }

        public static DisplayMode GetDisplayMode(string resolution, int monitor)
        {
            if (monitor < 0 || monitor > MonitorCount)
            {
                Logger.GetLogger()
                    .Log("Config-specified monitor isn't valid so we're going to use the system's primary display.",
                        LogLevel.Warning);
                monitor = 0;
            }

            if (ParseDisplayMode(resolution, out var width, out var height))
            {
                Logger.GetLogger().Log($"Available display modes for monitor {monitor} on {GraphicsCardDescription}:");

                var result = DisplayMode.Invalid;
                
                foreach (var mode in GetAvailableDisplayModes(monitor))
                {
                    Logger.GetLogger().Log($" - {mode.Width}x{mode.Height} ({mode.MonitorX}, {mode.MonitorY})");
                    if (result.IsInvalid && mode.Width == width && mode.Height == height)
                    {
                        result = mode;
                    }
                }

                if (result.IsInvalid)
                {
                    Logger.GetLogger()
                        .Log(
                            "Specified display mode " + resolution +
                            " not supported on this display, using default mode", LogLevel.Warning);
                    result = GetDefaultDisplayMode(monitor);
                }

                return result;
            }
            else
            {
                Logger.GetLogger()
                    .Log(
                        "Couldn't parse display resolution string " + resolution +
                        " into anything sensible - we're going to use the native screen resolution for this display.",
                        LogLevel.Warning);
                return GetDefaultDisplayMode(monitor);
            }
        }
        
        public static bool ParseDisplayMode(string displayMode, out int width, out int height)
        {
            var result = false;

            width = 0;
            height = 0;

            if (!string.IsNullOrWhiteSpace(displayMode))
            {
                var lowercase = displayMode.ToLower();

                var x = 'x';

                if (lowercase.Contains(x))
                {
                    var index = lowercase.IndexOf(x);

                    var wString = lowercase.Substring(0, index);
                    var hString = lowercase.Substring(index + 1);

                    if (int.TryParse(wString, out width) && int.TryParse(hString, out height))
                    {
                        result = true;
                    }
                }
            }
            
            return result;
        }

        [Cheat("GPUName")]
        private static void PrintGPUInfo()
        {
            Logger.GetLogger().Log(GraphicsCardDescription);
        }
    }
}