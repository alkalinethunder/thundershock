﻿using System;

namespace Thundershock.Debugging
{
    public class ConsoleOutput : ILogOutput
    {
        public bool Verbose { get; set; }
        
        public void Log(string message, LogLevel logLevel)
        {
            if (logLevel == LogLevel.Trace && !Verbose)
                return;

            var color = logLevel switch
            {
                LogLevel.Info => ConsoleColor.Gray,
                LogLevel.Message => ConsoleColor.Green,
                LogLevel.Warning => ConsoleColor.Magenta,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.DarkGray
            };

            Console.ForegroundColor = color;
            Console.WriteLine("LOG: {0}", message);
        }
    }
}