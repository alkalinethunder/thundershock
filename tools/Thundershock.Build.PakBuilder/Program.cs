using System;
using Thundershock.Core;

namespace Thundershock.Build.PakBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            EntryPoint.Run<PakBuilderApp>(args);
        }
    }
}
