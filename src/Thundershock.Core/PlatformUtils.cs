using System;
using Thundershock.Core.Debugging;
using Thundershock.Debugging;

namespace Thundershock.Core
{
    public static class PlatformUtils
    {
        private static IPlatform _platform;
        private static Logger _logger;
        
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
        
        public static void Destroy()
        {
            _logger = null;
            _platform = null;
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