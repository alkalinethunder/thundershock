using System;
using Gdk;
using Thundershock.Audio;
using Thundershock.Config;
using Thundershock.Core;
using Thundershock.OpenGL;

namespace Thundershock
{
    /// <summary>
    /// Provides all application functionality needed for a Thundershock Engine game.
    /// </summary>
    public abstract class NewGameAppBase : GraphicalAppBase
    {
        /// <inheritdoc />
        protected override void OnPreInit()
        {
            ConfigurationManager.ConfigurationLoaded += OnConfigurationLoaded;
            ApplyConfig();
            base.OnPreInit();
        }

        /// <inheritdoc />
        protected sealed override void OnPostInit()
        {
            base.OnPostInit();
            OnLoad();
        }

        private void OnConfigurationLoaded(object sender, EventArgs e)
        {
            ApplyConfig();
        }

        private void ApplyConfig()
        {
            Logger.Log("Configuration has been (re-)loaded.");

            // Set the BGM (MusicPlayer) volume
            MusicPlayer.MasterVolume = ConfigurationManager.ActiveConfig.BgmVolume;
            
            // v-sync
            Window.VSync = ConfigurationManager.ActiveConfig.VSync;
            
            // the configured screen resolution
            var displayMode = ConfigurationManager.GetDisplayMode();
            Logger.Log($"Display mode: {displayMode.Width}x{displayMode.Height} on monitor {displayMode.Monitor}");
            Logger.Log($"Full-screen: {ConfigurationManager.ActiveConfig.IsFullscreen}");
            Logger.Log($"V-sync: {ConfigurationManager.ActiveConfig.VSync}");
            Logger.Log($"Fixed time step: {ConfigurationManager.ActiveConfig.FixedTimeStepping}");
            Logger.Log($"Post-process Bloom: {ConfigurationManager.ActiveConfig.Effects.Bloom}");
            Logger.Log($"Post-process CRT Shadowmask: {ConfigurationManager.ActiveConfig.Effects.ShadowMask}");

            // input system
            SwapMouseButtons = ConfigurationManager.ActiveConfig.SwapMouseButtons;

            // post-processor settings.
            // Game.EnableBloom = config.ActiveConfig.Effects.Bloom;
            // Game.EnableShadowmask = config.ActiveConfig.Effects.ShadowMask;
            
            // fullscreen mode
            Window.IsFullScreen = ConfigurationManager.ActiveConfig.IsFullscreen;
            Window.IsBorderless = false;
            
            // Resolution change
            SetScreenSize(displayMode.Width, displayMode.Height);

            ApplyGraphicsChanges();
        }
        
        /// <summary>
        /// Called when it's time for the game to load.
        /// </summary>
        protected virtual void OnLoad() {}
    }
}