using Thundershock.Core;

namespace Thundershock
{
    public abstract class CommandLineApp : Application
    {
        protected abstract void Main();

        protected override void Bootstrap()
        {
            Main();
        }
    }
}
