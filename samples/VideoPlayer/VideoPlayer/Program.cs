using System;
using Thundershock.Core;

namespace VideoPlayer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            EntryPoint.Run<VideoPlayerApp>(args);
        }
    }
}
