using System;
using System.Collections.Generic;
using System.IO;
using Thundershock.IO;
using System.Text.Json;
using Thundershock.Core;
using Thundershock.Core.Debugging;

namespace Thundershock.Config
{
    /// <summary>
    /// Provides Thundershock-based applications with a central settings/configuration management system.
    /// </summary>
    [CheatAlias("Conf")]
    public static class ConfigurationManager
    {
        private static FileSystem _fs;
        private static GameConfiguration _gameConfig;

        /// <summary>
        /// This event is fired when the configuration file is loaded or re-loaded.
        /// </summary>
        public static event EventHandler ConfigurationLoaded;
        
        /// <summary>
        /// Gets an instance of <see cref="GameConfiguration"/> containing the current engine settings.
        /// </summary>
        public static GameConfiguration ActiveConfig => _gameConfig;

        /// <summary>
        /// Returns all display modes available for the current monitor.
        /// </summary>
        /// <returns>Enumerable list of current monitor's supported screen modes.</returns>
        /// <remarks>
        /// <para>
        /// When using this method, the engine will treat the <see cref="GameConfiguration.Monitor"/> property's
        /// current value as being the current monitor. If this value is invalid (the monitor index does not exist
        /// or was unplugged) then the engine's platform layer will report the supported screen modes of the user's
        /// primary monitor.
        /// </para>
        /// <para>
        /// When the application is in fullscreen mode, it's not recommended to set the screen resolution to a value
        /// NOT contained in this list. It may appear to work for you, however some graphics cards and displays may
        /// reject the request and cause a crash.
        /// </para>
        /// </remarks>
        public static IEnumerable<DisplayMode> GetAvailableDisplayModes()
        {
            return GamePlatform.GetAvailableDisplayModes(ActiveConfig.Monitor);
        }
        
        /// <summary>
        /// Gets the current screen mode.
        /// </summary>
        /// <returns>Returns the active display mode according to the configuration, or the primary screen's recommended display mode if the configured one is invalid.</returns>
        public static DisplayMode GetDisplayMode()
        {
            return GamePlatform.GetDisplayMode(ActiveConfig.Resolution, ActiveConfig.Monitor);
        }
        
        /// <summary>
        /// Discards the current configuration file and loads the engine's pre-programmed defaults.
        /// </summary>
        public static void ResetToDefaults()
        {
            _gameConfig = new GameConfiguration();
            SaveConfiguration();
            ApplyChanges();
        }
        
        /// <summary>
        /// Sets the screen mode to a resolution with the WxH format.
        /// </summary>
        /// <param name="value">The desired screen resolution in the form of WIDTHxHEIGHT.</param>
        /// <exception cref="InvalidOperationException">The resolution value could not be parsed.</exception>
        public static void SetDisplayMode(string value)
        {
            if (ParseDisplayMode(value, out int w, out int h))
            {
                _gameConfig.Resolution = $"{w}x{h}";
            }
            else
            {
                throw new InvalidOperationException(
                    $"\"{value}\" is not a properly-formatted display mode string. Must be <width>x<height>, e.x: 1920x1080");
            }
        }
        
        /// <summary>
        /// Applies and saves changes made to the current configuration.
        /// </summary>
        public static void ApplyChanges()
        {
            ConfigurationLoaded?.Invoke(null, EventArgs.Empty);
            SaveConfiguration();
        }

        /// <summary>
        /// Discards changes made to the current configuration and reloads the old one.
        /// </summary>
        public static void DiscardChanges()
        {
            LoadInitialConfig();
        }
        
        internal static void Initialize()
        {
            // Create the local data path if it does not already exist.
            if (!Directory.Exists(ThundershockPlatform.LocalDataPath))
                Directory.CreateDirectory(ThundershockPlatform.LocalDataPath);

            // Initialize a virtual file system for that path.
            _fs = FileSystem.FromHostDirectory(ThundershockPlatform.LocalDataPath);
            
            // Load the initial configuration.
            LoadInitialConfig();
        }
        
        internal static void Unload()
        {
            // Save the configuration.
            SaveConfiguration();
        }

        private static bool ParseDisplayMode(string displayMode, out int width, out int height)
        {
            var result = false;

            width = 0;
            height = 0;

            if (!string.IsNullOrWhiteSpace(displayMode))
            {
                var lowercase = displayMode.ToLower();

                var x = 'x';

                if (lowercase.Contains(x))
                {
                    var index = lowercase.IndexOf(x);

                    var wString = lowercase.Substring(0, index);
                    var hString = lowercase.Substring(index + 1);

                    if (int.TryParse(wString, out width) && int.TryParse(hString, out height))
                    {
                        result = true;
                    }
                }
            }
            
            return result;
        }
        
        private static void SaveConfiguration()
        {
            var json = JsonSerializer.Serialize(_gameConfig, typeof(GameConfiguration), new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true
            });
            _fs.WriteAllText("/config.json", json);
        }
        
        private static void LoadInitialConfig()
        {
            _gameConfig = null;

            if (_fs.FileExists("/config.json"))
            {
                var json = _fs.ReadAllText("/config.json");
                _gameConfig = JsonSerializer.Deserialize<GameConfiguration>(json, new JsonSerializerOptions
                {
                    IncludeFields = true
                });
            }
            else
            {
                _gameConfig = new GameConfiguration();
                
                // Save the configuration file
                SaveConfiguration();
            }

            ConfigurationLoaded?.Invoke(null, EventArgs.Empty);
        }

        [Cheat("Fullscreen")]
        internal static void Cheat_IsFullscreen(bool value)
        {
            ActiveConfig.IsFullscreen = value;
            ApplyChanges();
        }

        [Cheat("SetResolution")]
        internal static void Cheat_SetResolution(string value)
        {
            ActiveConfig.Resolution = value;
            ApplyChanges();
        }
    }
}