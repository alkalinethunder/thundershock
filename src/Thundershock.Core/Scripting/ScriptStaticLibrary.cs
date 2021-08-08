using System;

namespace Thundershock.Core.Scripting
{
    /// <summary>
    /// When added to a class, will mark that class as a static script library accessible with the given name.
    /// </summary>
    /// <remarks>
    /// This allows a C# class to be automatically exposed to all Thundershock JavaScript scripts. Because
    /// a direct static reference to the class can't be exposed, the engine will create a single instance of
    /// the class and expose that to JavaScript as a global variable with the name specified in this attribute.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ScriptStaticLibraryAttribute : Attribute
    {
        public string Name { get; }

        public ScriptStaticLibraryAttribute(string name)
        {
            Name = name;
        }
    }
}