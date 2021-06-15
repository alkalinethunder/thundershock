using System;

namespace Thundershock.Core
{
    public sealed class AppExitEventArgs : EventArgs
    {
        public bool CancelExit { get; private set; }

        public void Cancel()
        {
            CancelExit = true;
        }
    }
}
