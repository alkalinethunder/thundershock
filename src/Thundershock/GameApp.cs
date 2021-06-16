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
            
            base.OnPostInit();
        }
        
 
    }
}
