using System;

namespace Thundershock.Tweaker.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class TweakDescriptionAttribute : Attribute
    {
        public string Description { get; }

        public TweakDescriptionAttribute(string desc)
        {
            Description = desc;
        }
    }
}