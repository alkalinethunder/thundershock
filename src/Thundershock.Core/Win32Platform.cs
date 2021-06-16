using System.Runtime.InteropServices;
using Thundershock.Core.Debugging;
using Thundershock.Debugging;

namespace Thundershock.Core
{
    public class Win32Platform : IPlatform
    {
        #region Win32 methods

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        #endregion
        
        private Logger _logger;
        
        public void Initialize(Logger logger)
        {
            _logger = logger;
            
            // Workaround: JetBrains Rider output console doesn't work on Windows GUI applications.
            AttachConsole(-1);
            
            _logger.Log("Initializing Win32 Platform...");
        }

        public int GetMonitorCount()
        {
            throw new System.NotImplementedException();
        }

        public DisplayMode GetDefaultDisplayMode(int monitor)
        {
            throw new System.NotImplementedException();
        }

        public DisplayMode GetDisplayMode(int width, int height, int monitor)
        {
            throw new System.NotImplementedException();
        }
    }
}