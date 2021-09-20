using System;

namespace Thundershock.Tweaker.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class TweakNameAttribute : Attribute
    {
        public string Name { get; }

        public TweakNameAttribute(string name)
        {
            Name = name;
        }
    }
}