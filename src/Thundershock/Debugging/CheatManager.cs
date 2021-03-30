using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Thundershock.Debugging
{
    public class CheatManager : GlobalComponent
    {
        private List<CheatCode> _cheats = new List<CheatCode>();
        private ConcurrentQueue<string> _pendingCommands = new ConcurrentQueue<string>();

        public void ExecuteCommand(string commandLine)
        {
            if (!string.IsNullOrWhiteSpace(commandLine))
            {
                _pendingCommands.Enqueue(commandLine);
            }
        }

        public void AddCheat(string name, Action<string[]> action)
        {
            if (_cheats.Any(x => x.Name == name))
                throw new InvalidOperationException("Cheat code already registered.");

            _cheats.Add(new CheatCode(name, action));
        }
        
        public void AddCheat(string name, Action action)
        {
            AddCheat(name, _ => action());
        }
        
        private void ProcessCommand(string line)
        {
            try
            {
                var tokens = CommandShellUtils.BreakLine(line);

                if (!tokens.Any())
                    return;

                var name = tokens.First();
                var args = tokens.Skip(1).ToArray();

                var cmd = _cheats.FirstOrDefault(x => x.Name == name);
                if (cmd == null)
                {
                    App.Logger.Log($"Command not found: {name}", LogLevel.Warning);
                }
                else
                {
                    cmd.Call(args);
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogException(ex, LogLevel.Warning);
            }
        }
        
        protected override void OnUpdate(GameTime gameTime)
        {
            while (_pendingCommands.TryDequeue(out string cmd))
            {
                App.Logger.Log($"Executing: {cmd}");

                ProcessCommand(cmd);
            }
        }
    }
}