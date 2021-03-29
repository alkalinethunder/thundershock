using System.Collections.Concurrent;
using Microsoft.Xna.Framework;

namespace Thundershock.Debugging
{
    public class CheatManager : GlobalComponent
    {
        private ConcurrentQueue<string> _pendingCommands = new ConcurrentQueue<string>();

        public void ExecuteCommand(string commandLine)
        {
            if (!string.IsNullOrWhiteSpace(commandLine))
            {
                _pendingCommands.Enqueue(commandLine);
            }
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            while (_pendingCommands.TryDequeue(out string cmd))
            {
                App.Logger.Log($"Executing: {cmd}");
            }
        }
    }
}