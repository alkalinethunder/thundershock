using System;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Audio;
using Thundershock.Config;
using Thundershock.Content;
using Thundershock.Debugging;
using Thundershock.Input;

namespace Thundershock
{
    public abstract class GameApp : AppBase
    {
        private MonoGameLoop _game;
        private TimeSpan _uptime;
        private TimeSpan _frametime;

        public int ScreenWidth => _game.ScreenWidth;
        public int ScreenHeight => _game.ScreenHeight;
        public TimeSpan UpTime => _uptime;
        public TimeSpan FrameTime => _frametime;
        public GameWindow Window => _game.Window;
        public ContentManager Content => _game.Content;
        public ContentManager EngineContent => _game.EngineContent;
        public GraphicsDevice GraphicsDevice => _game.GraphicsDevice;

        
        public void LoadScene<T>() where T : Scene, new()
        { 
            Logger.Log($"Loading scene: {typeof(T).FullName}");
            _game.LoadScene<T>();
            Logger.Log("...done.");
        }

        protected sealed override void Bootstrap()
        {
            _game = new MonoGameLoop(this);
            _game.Run();
            _game.Dispose();
            _game = null;
        }

        public override void Exit()
            => _game.Exit();

        internal void Initialize()
        {
            // pre-init hook
            Logger.Log("PreInit hook reached.");
            OnPreInit();
            
            // REALLY IMPORTANT THAT WE DO THIS NOW
            RegisterComponent<PakManager>();
            RegisterComponent<ConfigurationManager>();
            
            // Not so important but most games will likely need the audio stuff.
            RegisterComponent<BgmManager>();
            
            // init hook
            Logger.Log("Init hook reached.");
            OnInit();
            
            // Initialize core components
            RegisterComponent<InputManager>();
            RegisterComponent<CheatManager>();
            
            // post-init hook
            Logger.Log("PostInit hook reached.");
            OnPostInit();
        }

        internal void Load()
        {
            // Load hook
            Logger.Log("Load hook reached.");
            OnLoad();
        }

        internal void Unload()
        {
            // Pre-unload hook
            Logger.Log("PreUnload hook reached.");
            OnPreUnload();
            
            // Unload all global components.
            this.UnloadAllComponents();
            
            // Unload hook
            Logger.Log("Unload hook reached.");
            OnUnload();
            
            // Post-unload hook
            Logger.Log("PostUnload hook reached.");
            OnPostUnload();
        }

        internal void Update(GameTime gameTime)
        {
            // Action queue.
            this.RunQueuedActions();
            
            // warn the user if frame time is excessively long
            if (gameTime.ElapsedGameTime.TotalSeconds >= 0.25)
            {
                Logger.Log(
                    $"It appears that the last engine update took longer than 0.25 seconds ({gameTime.ElapsedGameTime.TotalSeconds}s).",
                    LogLevel.Warning);
                Logger.Log(
                    "Are any components, scenes or scene components doing heavy work on their OnUpdate methods?",
                    LogLevel.Warning);
            }
            
            // up-time and frame-time update
            _uptime = gameTime.TotalGameTime;
            _frametime = gameTime.ElapsedGameTime;
            
            // Update all global components.
            this.UpdateComponents(gameTime);
        }
        
        #region App hooks
        protected virtual void OnPreInit() {}
        protected virtual void OnInit() {}
        protected virtual void OnPostInit() {}
        protected virtual void OnLoad() {}
        protected virtual void OnPreUnload() {}
        protected virtual void OnUnload() {}
        protected virtual void OnPostUnload() {}
        #endregion
    }
}