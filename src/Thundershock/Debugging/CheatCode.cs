using System;

namespace Thundershock.Debugging
{
    public class CheatCode
    {
        private Action<string[]> _action;
        
        public string Name { get; }

        public CheatCode(string name, Action<string[]> action)
        {
            Name = name;
            _action = action;
        }
        
        public void Call(string[] args)
            => _action?.Invoke(args);
    }
}