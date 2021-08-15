using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Thundershock.Core;
using Thundershock.Core.Debugging;

namespace Thundershock.Debugging
{
    public class CheatManager : GlobalComponent
    {
        private List<CheatCode> _cheats = new List<CheatCode>();
        private ConcurrentQueue<string> _pendingCommands = new ConcurrentQueue<string>();

        public void AddObject(object obj)
        {
            var type = obj.GetType();

            var aliasAttribute = type.GetCustomAttributes(true).OfType<CheatAliasAttribute>().FirstOrDefault();

            var name = (aliasAttribute != null && !string.IsNullOrWhiteSpace(aliasAttribute.Name))
                ? aliasAttribute.Name
                : type.Name;

            foreach (var method in
                type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var cheatAttrib = method.GetCustomAttributes(true).OfType<CheatAttribute>().FirstOrDefault();

                if (cheatAttrib == null)
                    continue;

                var cmdName = string.IsNullOrWhiteSpace(cheatAttrib.Name) ? method.Name : cheatAttrib.Name;

                var fullname = $"{name}.{cmdName}";

                AddCheat(fullname, method, obj);
            }
        }

        public void RemoveObject(object obj)
        {
            _cheats.RemoveAll(x => x.Instance == obj);
        }
        
        public void ExecuteCommand(string commandLine)
        {
            if (!string.IsNullOrWhiteSpace(commandLine))
            {
                _pendingCommands.Enqueue(commandLine);
            }
        }

        public void AddCheat(string name, MethodInfo method, object instance = null)
        {
            if (_cheats.Any(x => x.Name == name))
                return;

            _cheats.Add(new CheatCode(name, method, instance));
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

        protected override void OnLoad()
        {
            base.OnLoad();
            
            // Add cheats from common Thundershock objects
            AddObject(this); // Help
            AddObject(App); // App control.
            AddObject(Logger.GetLogger()); // Console control
            
            // Static cheats
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = Array.Empty<Type>();

                try
                {
                    types = ass.GetTypes();
                }
                catch (Exception ex)
                {
                    App.Logger.LogException(ex);
                }
                
                foreach (var type in types)
                {
                    var cheatAlias = type.GetCustomAttributes(false).OfType<CheatAliasAttribute>().FirstOrDefault();

                    if (cheatAlias == null)
                        continue;

                    var name = cheatAlias.Name;

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    foreach (var method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        var cheatAttrib = method.GetCustomAttributes(false).OfType<CheatAttribute>().FirstOrDefault();

                        if (cheatAttrib == null)
                            continue;

                        var cheatName = string.IsNullOrWhiteSpace(cheatAttrib.Name) ? method.Name : cheatAttrib.Name;

                        var fullname = $"{name}.{cheatName}";

                        AddCheat(fullname, method);
                    }
                }
            }
        }

        [Cheat]
        public void Help()
        {
            Logger.GetLogger().Log("Available commands:");
            Logger.GetLogger().Log("");
            foreach (var command in _cheats.OrderBy(x => x.Name))
            {
                Logger.GetLogger().Log(" - " + command.Name);
            }
        }
    }
}