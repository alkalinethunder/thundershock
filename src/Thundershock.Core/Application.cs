using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Thundershock.Core.Debugging;

namespace Thundershock.Core
{
    /// <summary>
    /// Provides the base functionality for all applications written for Thundershock.
    /// </summary>
    public abstract class Application
    {
        private ConcurrentQueue<Action> _actionQueue = new();

        /// <summary>
        /// Submit a method to run on the next Thundershock engine tick. Useful for performing thread-unsafe operations from outside the game thread.
        /// </summary>
        /// <param name="action">The method to run.</param>
        public void EnqueueAction(Action action)
        {
            _actionQueue.Enqueue(action);
        }
        
        /// <summary>
        /// Performs an application exit request and returns the result of the request.
        /// </summary>
        /// <returns>True if the engine fulfilled the request, false if the exit was cancelled by the running application.</returns>
        public bool Exit()
        {
            Logger.Log("Exit requested.");

            Logger.Log("Allowing the app to do stuff before exiting...");
            var exitEvent = new AppExitEventArgs();
            BeforeExit(exitEvent);

            if (exitEvent.CancelExit)
                Logger.Log("App has cancelled the exit event.");

            return !exitEvent.CancelExit;
        }
        
        public void Run()
        {
            Bootstrap();

            GamePlatform.FinalShutdown();
        }
        
        protected void RunQueuedActions()
        {
            while (_actionQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        protected abstract void Bootstrap();
        protected virtual void BeforeExit(AppExitEventArgs args) {}
    }
}
