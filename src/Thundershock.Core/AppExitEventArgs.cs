using System;

namespace Thundershock.Core
{
    /// <summary>
    /// Contains the event parameters for a Thundershock application exit event.
    /// </summary>
    public sealed class AppExitEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a value indicating whether the exit operation has been cancelled.
        /// </summary>
        public bool CancelExit { get; private set; }
        
        /// <summary>
        /// Cancels the exit operation.
        /// </summary>
        public void Cancel()
        {
            CancelExit = true;
        }
    }
}
