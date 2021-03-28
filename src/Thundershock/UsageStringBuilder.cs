using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DocoptNet;

namespace Thundershock
{
    public class UsageStringBuilder
    {
        private string _command;
        private string _summary;
        private List<Flag> _flags = new List<Flag>();
        private List<string> _actions = new List<string>();
        
        public string Command
        {
            get => _command;
            set => _command = value;
        }

        public string Description
        {
            get => _summary;
            set => _summary = value;
        }

        public bool AllowNoArguments { get; set; } = true;
        
        public UsageStringBuilder(string command)
        {
            _command = command;
        }

        public void AddFlag(char flag, string name, string description, Action<bool> setter)
        {
            if (_flags.Any(x => x.Char == flag || x.Name == name))
                throw new InvalidOperationException("A flag with that name or character already exists.");

            _flags.Add(new Flag
            {
                Char = flag,
                Name = name,
                Description = description,
                Setter = setter
            });
        }

        public void AddAction(string action)
        {
            _actions.Add(action);
        }
        
        public string GetString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(_command);
            sb.AppendLine();
            
            if (!string.IsNullOrWhiteSpace(_summary))
            {
                sb.AppendLine(_summary);
                sb.AppendLine();
            }

            sb.AppendLine("usage:");

            if (AllowNoArguments || !_actions.Any())
            {
                sb.Append("    " + _command);
                if (_flags.Any())
                    sb.Append(" [options]");
                sb.AppendLine();
            }

            foreach (var action in _actions)
            {
                sb.Append("    " + _command + " " + action);
                if (_flags.Any())
                    sb.Append(" [options]");
                sb.AppendLine();
            }

            if (_flags.Any())
            {
                var longestLength = _flags.Select(x => $"-{x.Char} | --{x.Name}".Length).OrderByDescending(x => x)
                    .First() + 6;
                
                sb.AppendLine();
                sb.AppendLine("options: ");
                foreach (var flag in _flags.OrderBy(x => x.Name))
                {
                    var name = $"-{flag.Char} | --{flag.Name}";
                    sb.Append("    " + name);
                    for (var i = 0; i < longestLength - name.Length; i++)
                        sb.Append(" ");
                    sb.Append(flag.Description);
                    sb.AppendLine();
                }
            }
            
            return sb.ToString();
        }

        public void Apply(string[] args)
        {
            var usage = GetString();
            var doc = new Docopt();
            var results = doc.Apply(usage, args);

            foreach (var flag in _flags.Where(x => results["--" + x.Name] != null))
                flag.Setter(true);
        }
        
        private class Flag
        {
            public char Char;
            public string Name;
            public string Description;
            public Action<bool> Setter;
        }
    }
}