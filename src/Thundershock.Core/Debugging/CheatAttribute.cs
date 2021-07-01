using System;

namespace Thundershock.Core.Debugging
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CheatAttribute : Attribute
    {
        public string Description { get; set; }
        public string Name { get; }

        public CheatAttribute(string name = null)
        {
            Name = name ?? string.Empty;
        }
    }
}