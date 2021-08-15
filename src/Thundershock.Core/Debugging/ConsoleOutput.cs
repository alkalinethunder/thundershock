using System;
#if DEBUG
using System.Diagnostics;
#endif

namespace Thundershock.Core.Debugging
{
    /// <summary>
    /// Implements system console output for the <see cref="Logger"/>.
    /// </summary>
    public class ConsoleOutput : ILogOutput
    {
        /// <summary>
        /// Gets or sets a value indicating whether verbose logging is enabled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this property is set to true, then trace logs will be outputted to the console.
        /// Because trace logs are frequent and several of them can come out every frame when debugging
        /// the engine, this should only be turned on when you need to trace what's going on. Otherwise
        /// the console will get spammed with random-ass OpenGL crap that you likely don't care about.
        /// </para>
        /// </remarks>
        public bool Verbose { get; set; }
        
        /// <inheritdoc />
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

            // Workaround for Rider 2021.1 being dumb and not fucking displaying
            // the console.
#if DEBUG
            Debug.WriteLine(message);
#endif
        }
    }
}