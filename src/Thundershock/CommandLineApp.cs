using System;

namespace Thundershock
{
    public abstract class CommandLineApp : AppBase
    {
        protected abstract void Main();
        
        protected override void Bootstrap()
        {
            Main();
        }

        public override void Exit()
        {
            Environment.Exit(0);
        }
    }
}