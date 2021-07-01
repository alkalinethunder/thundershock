using System;
using System.Reflection;
using Thundershock.Core;

namespace Thundershock.Debugging
{
    public class CheatCode
    {
        public MethodInfo Method { get; }
        public object Instance { get; }
        
        public string Name { get; }

        public CheatCode(string name, MethodInfo method, object instance)
        {
            Name = name;
            Method = method;
            Instance = instance;
        }

        public void Call(string[] args)
        {
            var paramList = CommandLineHelpers.ParseParameterList(args, Method);
            Method.Invoke(Instance, paramList);
        }
    }
}