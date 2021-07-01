using System;

namespace Thundershock.Core.Input
{
    public class KeyEventArgs : EventArgs
    {
        public Keys Key { get; }

        public KeyEventArgs(Keys key)
        {
            Key = key;
        }
    }
}