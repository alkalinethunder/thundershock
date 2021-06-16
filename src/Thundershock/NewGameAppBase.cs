using System;
using Thundershock.Config;
using Thundershock.Core;
using Thundershock.OpenGL;

namespace Thundershock
{
    public class NewGameAppBase : GraphicalAppBase
    {
        protected override GameWindow CreateGameWindow()
        {
            return new SDLGameWindow();
        }

        protected override void OnPreInit()
        {
            // Set up the game platform.
            PlatformUtils.InitializeGame(new SDLGamePlatform());
            
            GetComponent<ConfigurationManager>().ConfigurationLoaded += OnConfigurationLoaded;
            ApplyConfig();
            base.OnPreInit();
        }

        private void OnConfigurationLoaded(object? sender, EventArgs e)
        {
            ApplyConfig();
        }

        private void ApplyConfig()
        {
            Logger.Log("Configuration has been (re-)loaded.");
            var config = GetComponent<ConfigurationManager>();

            // the configured screen resolution
            var displayMode = config.GetNewDisplayMode();
            Logger.Log($"Display mode: {displayMode.Width}x{displayMode.Height} on monitor {displayMode.Monitor}");
            Logger.Log($"Full-screen: {config.ActiveConfig.IsFullscreen}");
            Logger.Log($"V-sync: {config.ActiveConfig.VSync}");
            Logger.Log($"Fixed time step: {config.ActiveConfig.FixedTimeStepping}");
            Logger.Log($"Post-process Bloom: {config.ActiveConfig.Effects.Bloom}");
            Logger.Log($"Post-process CRT Shadowmask: {config.ActiveConfig.Effects.ShadowMask}");

            // post-processor settings.
            // Game.EnableBloom = config.ActiveConfig.Effects.Bloom;
            // Game.EnableShadowmask = config.ActiveConfig.Effects.ShadowMask;

            // should we reset the gpu?
            var applyGraphicsChanges = false;

            // Resolution change
            if (ScreenWidth != displayMode.Width || ScreenHeight != displayMode.Height)
            {
                SetScreenSize(displayMode.Width, displayMode.Height);
                applyGraphicsChanges = true;
            }

            // v-sync
            // if (Game.VSync != config.ActiveConfig.VSync)
            // {
                // Game.VSync = config.ActiveConfig.VSync;
                // applyGraphicsChanges = true;
            // }

            // fixed time stepping
            // if (Game.IsFixedTimeStep != config.ActiveConfig.FixedTimeStepping)
            // {
                // Game.IsFixedTimeStep = config.ActiveConfig.FixedTimeStepping;
                // applyGraphicsChanges = true;
            // }

            // fullscreen mode
            if (IsFullScreen != config.ActiveConfig.IsFullscreen)
            {
                IsFullScreen = config.ActiveConfig.IsFullscreen;
                applyGraphicsChanges = true;
            }

            // update the GPU if we need to
            if (applyGraphicsChanges)
            {
                ApplyGraphicsChanges();
            }
        }
    }
}