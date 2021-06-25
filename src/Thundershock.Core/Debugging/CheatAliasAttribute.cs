using System;

namespace Thundershock.Core.Debugging
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CheatAliasAttribute : Attribute
    {
        public string Name { get; }

        public CheatAliasAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}