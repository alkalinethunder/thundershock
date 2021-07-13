namespace Thundershock.Core.Debugging
{
    public interface ILogOutput
    {
        void Log(string message, LogLevel logLevel);
    }
}