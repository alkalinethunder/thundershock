using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.Linq;
using Thundershock.Config;
using Thundershock.Input;

namespace Thundershock
{
    public abstract class App
    {
        private List<GlobalComponent> _components = new List<GlobalComponent>();
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
        
        public void LoadScene<T>() where T : Scene, new()
        {
            _game.LoadScene<T>();
        }

        public T GetComponent<T>() where T : GlobalComponent, new()
        {
            return _components.OfType<T>().FirstOrDefault() ?? RegisterComponent<T>();
        }

        internal void Initialize(MonoGameLoop game)
        {
            if (_game != null)
                throw new InvalidOperationException("App has already been initialized.");

            // pre-init hook
            OnPreInit();
            
            _game = game ?? throw new ArgumentNullException(nameof(game));
            
            // REALLY IMPORTANT THAT WE DO THIS NOW
            RegisterComponent<ConfigurationManager>();
            
            // init hook
            OnInit();
            
            // Initialize core components
            RegisterComponent<InputManager>();
            
            // post-init hook
            OnPostInit();
        }

        internal void Load()
        {
            // Load hook
            OnLoad();
        }

        internal void Unload()
        {
            // Pre-unload hook
            OnPreUnload();
            
            // Unload all global components.
            while (_components.Any())
            {
                _components.First().Unload();
                _components.RemoveAt(0);
            }

            // Unload hook
            OnUnload();
            
            // unbind from the game
            _game = null;
            
            // Post-unload hook
            OnPostUnload();
        }

        internal void Update(GameTime gameTime)
        {
            // up-time and frame-time update
            _uptime = gameTime.TotalGameTime;
            _frametime = gameTime.ElapsedGameTime;
            
            // Update all global components.
            foreach (var component in _components.ToArray())
            {   
                component.Update(gameTime);
            }
        }
        
        protected T RegisterComponent<T>() where T : GlobalComponent, new()
        {
            if (_components.Any(x => x is T))
                throw new InvalidOperationException("Component is already registered.");
            
            var instance = new T();
            _components.Add(instance);
            instance.Initialize(this);
            return instance;
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