namespace Thundershock.Core
{
    public interface IGamePlatform
    {
        int GetMonitorCount();
        DisplayMode GetDefaultDisplayMode(int monitor);
    }
}