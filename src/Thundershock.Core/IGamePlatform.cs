namespace Thundershock.Core
{
    public interface IGamePlatform
    {
        int GetMonitorCount();
        DisplayMode GetDefaultDisplayMode(int monitor);
        
        string GraphicsCardDescription { get; }
    }

    public static class GamePlatform
    {
        private static IGamePlatform _gamePlatform;

        public static string GraphicsCardDescription => _gamePlatform.GraphicsCardDescription;

        public static void Initialize(IGamePlatform gamePlatform)
        {
            _gamePlatform = gamePlatform;
        }
    }
}