using System;

namespace Thundershock.Tweaker.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class TweakCategoryAttribute : Attribute
    {
        public string Category { get; }

        public TweakCategoryAttribute(string category)
        {
            Category = category;
        }
    }
}