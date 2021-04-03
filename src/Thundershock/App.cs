using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Config;
using Thundershock.Debugging;
using Thundershock.Input;

namespace Thundershock
{
    public abstract class App
    {
        private List<GlobalComponent> _components = new List<GlobalComponent>();
        private MonoGameLoop _game;
        private TimeSpan _uptime;
        private TimeSpan _frametime;
        private Logger _logger;

        public Logger Logger
        {
            get => _logger;
            internal set => _logger = value;
        }

        public void Exit()
            => _game.Exit();
        
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
            _logger.Log($"Loading scene: {typeof(T).FullName}");
            _game.LoadScene<T>();
            _logger.Log("...done.");
        }

        public T GetComponent<T>() where T : GlobalComponent, new()
        {
            return _components.OfType<T>().First() ?? RegisterComponent<T>();
        }

        internal void Initialize(MonoGameLoop game)
        {
            if (_game != null)
                throw new InvalidOperationException("App has already been initialized.");

            // pre-init hook
            _logger.Log("PreInit hook reached.");
            OnPreInit();
            
            _game = game ?? throw new ArgumentNullException(nameof(game));
            
            // REALLY IMPORTANT THAT WE DO THIS NOW
            RegisterComponent<ConfigurationManager>();
            
            // init hook
            _logger.Log("Init hook reached.");
            OnInit();
            
            // Initialize core components
            RegisterComponent<InputManager>();
            RegisterComponent<CheatManager>();
            
            // post-init hook
            _logger.Log("PostInit hook reached.");
            OnPostInit();
        }

        internal void Load()
        {
            // Load hook
            _logger.Log("Load hook reached.");
            OnLoad();
        }

        internal void Unload()
        {
            // Pre-unload hook
            _logger.Log("PreUnload hook reached.");
            OnPreUnload();
            
            // Unload all global components.
            while (_components.Any())
            {
                _components.First().Unload();
                _components.RemoveAt(0);
            }

            // Unload hook
            _logger.Log("Unload hook reached.");
            OnUnload();
            
            // unbind from the game
            _game = null;
            
            // Post-unload hook
            _logger.Log("PostUnload hook reached.");
            OnPostUnload();
        }

        internal void Update(GameTime gameTime)
        {
            // warn the user if frame time is excessively long
            if (gameTime.ElapsedGameTime.TotalSeconds >= 0.25)
            {
                _logger.Log(
                    $"It appears that the last engine update took longer than 0.25 seconds ({gameTime.ElapsedGameTime.TotalSeconds}s).",
                    LogLevel.Warning);
                _logger.Log(
                    "Are any components, scenes or scene components doing heavy work on their OnUpdate methods?",
                    LogLevel.Warning);
            }
            
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

            _logger.Log($"Registering global component: {typeof(T).FullName}");
            
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