using System;

namespace Thundershock.Core.Scripting
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false)]
    public class ScriptTypeAttribute : Attribute
    {
        public string Name { get; }

        public ScriptTypeAttribute(string name = null)
        {
            Name = name;
        }
    }
}