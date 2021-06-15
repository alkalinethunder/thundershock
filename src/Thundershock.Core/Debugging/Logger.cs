using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Thundershock.Debugging;

namespace Thundershock.Core.Debugging
{
    public class Logger
    {
        private List<ILogOutput> _outputs = new List<ILogOutput>();
        
        private void All(Action<ILogOutput> action)
        {
            foreach (var output in _outputs)
                action(output);
        }

        public void RemoveOutput(ILogOutput output)
        {
            _outputs.Remove(output);
        }

        public void AddOutput(ILogOutput output)
        {
            _outputs.Add(output);
            Log($"Added logger output: {output.ToString()}");
        }

        public void Log(string message, LogLevel logLevel = LogLevel.Info, [CallerMemberName] string member = "", [CallerLineNumber] int ln = 0, [CallerFilePath] string path = "")
        {
            var fname = Path.GetFileName(path);
            var frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;
            var typeName = type.Name;

            var formatted = string.Format("[{0}] <{1}:{2}/{3}> {4}::{5}(): {6}", DateTime.Now.ToLongTimeString(), fname,
                ln, logLevel.ToString().ToLower(), typeName, member, message);

            All(x=>x.Log(formatted, logLevel));
        }

        public void LogException(Exception ex, LogLevel logLevel = LogLevel.Error)
        {
            foreach (var line in ex.ToString().Split(Environment.NewLine))
                Log(line, logLevel);
        }
    }
}
