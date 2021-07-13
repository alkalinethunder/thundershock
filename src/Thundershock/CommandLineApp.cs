using Thundershock.Core;

namespace Thundershock
{
    public abstract class CommandLineApp : AppBase
    {
        protected abstract void Main();

        protected override void Bootstrap()
        {
            Main();
        }
    }
}
