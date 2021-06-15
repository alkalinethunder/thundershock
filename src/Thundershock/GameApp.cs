using System;
using Thundershock.Audio;
using Thundershock.Config;
using Thundershock.Content;

namespace Thundershock
{
    public abstract class GameApp : GameAppBase
    {
        protected sealed override void OnPreInit()
        {
            // Start up the PakManager.
            RegisterComponent<PakManager>();

            // Enable post-processing.
            Game.AllowPostProcessing = true;

            base.OnPreInit();
        }

        protected override void OnPostInit()
        {
            // Register the BgmManager now that we have pakdata mounted.
            RegisterComponent<BgmManager>();

            // It's up to us to get the config manager to update the game config.
            GetComponent<ConfigurationManager>().ConfigurationLoaded += OnConfigurationLoaded;

            base.OnPostInit();
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
            var displayMode = config.GetDisplayMode();
            Logger.Log($"Display mode: {displayMode.Width}x{displayMode.Height}");
            Logger.Log($"Full-screen: {config.ActiveConfig.IsFullscreen}");
            Logger.Log($"V-sync: {config.ActiveConfig.VSync}");
            Logger.Log($"Fixed time step: {config.ActiveConfig.FixedTimeStepping}");
            Logger.Log($"Post-process Bloom: {config.ActiveConfig.Effects.Bloom}");
            Logger.Log($"Post-process CRT Shadowmask: {config.ActiveConfig.Effects.ShadowMask}");

            // post-processor settings.
            Game.EnableBloom = config.ActiveConfig.Effects.Bloom;
            Game.EnableShadowmask = config.ActiveConfig.Effects.ShadowMask;

            // should we reset the gpu?
            var applyGraphicsChanges = false;

            // Resolution change
            if (Game.ScreenWidth != displayMode.Width || Game.ScreenHeight != displayMode.Height)
            {
                Game.SetScreenSize(displayMode.Width, displayMode.Height);
                applyGraphicsChanges = true;
            }

            // v-sync
            if (Game.VSync != config.ActiveConfig.VSync)
            {
                Game.VSync = config.ActiveConfig.VSync;
                applyGraphicsChanges = true;
            }

            // fixed time stepping
            if (Game.IsFixedTimeStep != config.ActiveConfig.FixedTimeStepping)
            {
                Game.IsFixedTimeStep = config.ActiveConfig.FixedTimeStepping;
                applyGraphicsChanges = true;
            }

            // fullscreen mode
            if (Game.IsFullScreen != config.ActiveConfig.IsFullscreen)
            {
                Game.IsFullScreen = config.ActiveConfig.IsFullscreen;
                applyGraphicsChanges = true;
            }

            // update the GPU if we need to
            if (applyGraphicsChanges)
            {
                Game.ApplyDisplayChanges();
            }
        }

    }
}
