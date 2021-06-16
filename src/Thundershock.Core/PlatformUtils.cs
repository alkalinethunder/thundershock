using System;
using Thundershock.Core.Debugging;
using Thundershock.Debugging;

namespace Thundershock.Core
{
    public static class PlatformUtils
    {
        private static IPlatform _platform;
        private static Logger _logger;
        private static IGamePlatform _gamePlatform;
        
        public static void Initialize(Logger logger)
        {
            if (_platform != null)
                throw new InvalidOperationException(
                    "Cannot initialize the platform utilities class. It's already been initialized.");

            _logger = logger;

            if (!TryCreatePlatform())
                throw new InvalidOperationException("Platform not supported.");
            
            _platform.Initialize(logger);
        }

        public static int MonitorCount => _platform.GetMonitorCount();

        public static void InitializeGame(IGamePlatform gamePlatform)
        {
            if (_gamePlatform != null)
                throw new InvalidOperationException("Game platform already initialized.");
            _gamePlatform = gamePlatform;
        }
        
        public static void Destroy()
        {
            _gamePlatform = null;
            _logger = null;
            _platform = null;
        }

        public static DisplayMode GetDisplayMode(string resolution, int monitor = 0)
        {
            var displayMode = null as DisplayMode;

            if (_gamePlatform != null)
            {
                var monCount = _gamePlatform.GetMonitorCount();
                if (monitor < 0 || monitor > monCount)
                    monitor = 0;

                displayMode = _gamePlatform.GetDefaultDisplayMode(monitor);
            }
            else
            {
                _logger.Log(
                    "No game platform was initialized so we're going to fallback to 1024x768 @ 0,0 as a default display mode.",
                    LogLevel.Warning);
                displayMode = new(1024,768,0,0,0);
            }
            
            if (ParseDisplayMode(resolution, out var w, out var h))
            {
                displayMode.Width = w;
                displayMode.Height = h;
            }

            return displayMode;
        }

        private static bool ParseDisplayMode(string displayMode, out int width, out int height)
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
        
        private static bool TryCreatePlatform()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                _platform = new Win32Platform();
                return true;
            }

            _logger.Log("No platform support object found for this system. Thundershock will crash soon.",
                LogLevel.Fatal);
            return false;
        }
    }
}