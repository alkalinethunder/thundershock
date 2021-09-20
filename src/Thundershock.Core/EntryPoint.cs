using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Thundershock.Core.Debugging;

namespace Thundershock.Core
{
    public static class EntryPoint
    {
        private static Dictionary<string, Type> _entryPoints = new Dictionary<string, Type>();
        private static Application _current;
        private static Assembly _entryAssembly;
        private static EntryArgs _entryArgs;

        public static Assembly EntryAssembly => _entryAssembly;
        public static Application CurrentApp => _current;

        public static bool GetBoolean(EntryBoolean boolean)
        {
            if (_entryArgs == null)
                return false;

            return boolean switch
            {
                EntryBoolean.VerboseLog => _entryArgs.Verbose,
                EntryBoolean.WipeConfig => _entryArgs.WipeConfig,
                EntryBoolean.SkipConfig => _entryArgs.SkipConfig,
                EntryBoolean.MuteAudio => _entryArgs.MuteAudio,
                EntryBoolean.GuiDebug => _entryArgs.LayoutDebug,
                EntryBoolean.DisablePostProcessing => _entryArgs.DisablePostProcessor,
                _ => false
            };
        }
        
        public static void RegisterApp<T>(string appName) where T : Application, new()
        {
            if (string.IsNullOrWhiteSpace(appName))
                throw new FormatException(nameof(appName));
            
            if (_entryPoints.ContainsKey(appName))
                throw new InvalidOperationException("The given application name is already registered.");
            
            _entryPoints.Add(appName, typeof(T));
        }

        private static EntryArgs GetEntryArgs(string cmd, string[] args)
        {
            // result
            var entryArgs = new EntryArgs();
            
            // usage string builder
            var builder = new UsageStringBuilder(cmd);

            foreach (var key in _entryPoints.Keys)
                builder.AddAction(key, x => entryArgs.AppEntry = x);

            // setting overrides
            builder.AddFlag('m', "mute-audio", "Completely mute all audio players.", x => entryArgs.MuteAudio = x);
            builder.AddFlag('C', "skip-config", "Skip loading the game's user configuration.",
                x => entryArgs.SkipConfig = x);
            builder.AddFlag('p', "no-postprocessing", "Shut off the Thundershock post-processor.",
                x => entryArgs.DisablePostProcessor = x);
            builder.AddFlag('v', "verbose", "Verbose engine logging to console.", x => entryArgs.Verbose = x);
            builder.AddFlag('w', "wipe-user-data",
                "COMPLETELY WIPE ALL USER DATA FOR THE GAME BEING RUN. Thundershock will not ask your permission before doing so.", x => entryArgs.WipeConfig = x);
            builder.AddFlag('l', "layout-debug", "GUI system layout debugger enable", x => entryArgs.LayoutDebug = x);
            
            // Apply the command-line arguments to the usage string.
            builder.Apply(args);

            return entryArgs;
        }
        
        public static void Run<T>(string[] args) where T : Application, new()
        {
            // Initialize the LOgger
            Logger.Initialize();
            
            // Retrieve the entry-point assembly. This is important for building the docopt usage string.
            var entryPointAssembly = Assembly.GetEntryAssembly();
            
            // Null-check that assembly.
            if (entryPointAssembly == null)
            {
                Logger.Log("Could not find entry-point assembly information.", LogLevel.Fatal);
                Environment.Exit(-1);
                return;
            }
            
            _entryAssembly = entryPointAssembly;
            
            // Get the file name.
            var entryPointFileName = Path.GetFileName(entryPointAssembly.Location);

            // Get entry arguments
            var entryArgs = GetEntryArgs(entryPointFileName, args);
            
            // Keep track of our entrypoint args.
            _entryArgs = entryArgs;

            // create the logger and set up the default outputs
            var console = new ConsoleOutput();
            Logger.AddOutput(console);
            
            // verbose logging
            console.Verbose = entryArgs.Verbose;
            if (console.Verbose)
                Logger.Log("--verbose flag specified, verbose console logging is on.");
            
            // the app we're going to run
            var app = null as Application;

            // Did the entry args specify an entry point?
            if (!string.IsNullOrWhiteSpace(entryArgs.AppEntry))
            {
                // create the app specified
                app = (Application) Activator.CreateInstance(_entryPoints[entryArgs.AppEntry], null);
            }
            else
            {
                // Create the default app.
                app = new T();
            }

            Logger.Log("Created new app: " + app.GetType().FullName);
            
            // *a distant rumble occurs in the distance, followed by a flash of light*
            Bootstrap(app);
            
            // we're done.
            Logger.Log("Thundershock has been torn down.");
            _entryAssembly = null;
            _entryArgs = null;
        }

        private static void Bootstrap(Application app)
        {
            if (_current != null)
                throw new InvalidOperationException("Failed to bootstrap the app. Thundershock is already running.");

            // bind this thundershock app to this instance of thundershock.
            _current = app;
            Logger.Log($"Bootstrapping \"{_current.GetType().Name}\"...");
            
            // Hand control off to the app.
            // We no longer need to worry about starting MonoGame - the app has COMPLETE control
            // over what it decides to use as a rendering system.
            app.Run();
            
            // The above method blocks until MonoGame tears itself down successfully. If we get this far, we can unbind the app.
            _current = null;
            Logger.Log("The boots were off and the straps are undone, the app is no longer being run.");
        }

        private class EntryArgs
        {
            public bool SkipConfig;
            public bool MuteAudio;
            public bool DisablePostProcessor;
            public bool Verbose;
            public bool WipeConfig;
            public bool LayoutDebug;

            public string AppEntry;
        }
    }
}
