using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Components;
using Thundershock.Debugging;
using Thundershock.Rendering;

namespace Thundershock
{
    public abstract class Scene
    {
        private GameApp _app;
        private MonoGameLoop _gameLoop;
        private List<SceneComponent> _components = new List<SceneComponent>();
        private SpriteFont _debugFont;
        private WarningPrinter _warningPrinter;
        private DeveloperConsole _devConsole;
        
        public Camera Camera { get; protected set; }

        public Rectangle ViewportBounds
            => Camera != null ? Camera.ViewportBounds : Rectangle.Empty;
        
        public GameApp App => _app;
        public MonoGameLoop Game => _gameLoop;

        public bool HasComponent<T>() where T : SceneComponent
        {
            return _components.Any(x => x is T);
        }
        
        public void AddComponent(SceneComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));
            if (component.Scene != null)
                throw new InvalidOperationException("Scene component already belongs to a Scene.");
            _app.Logger.Log($"Adding new component: {component}");
            _components.Add(component);
            component.Load(this);
        }

        public T GetComponent<T>() where T : SceneComponent
        {
            return _components.OfType<T>().First();
        }
        
        public T AddComponent<T>() where T : SceneComponent, new()
        {
            var component = new T();
            AddComponent(component);
            return component;
        }

        public void RemoveComponent(SceneComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            _app.Logger.Log($"Unloading component: {component}");
            component.Unload();
            _components.Remove(component);
        }
        
        internal void Load(GameApp app, MonoGameLoop gameLoop)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _gameLoop = gameLoop ?? throw new ArgumentNullException(nameof(gameLoop));
            _app.Logger.Log("OnLoad reached.");
            _debugFont = App.EngineContent.Load<SpriteFont>("Fonts/DebugSmall");
            OnLoad();
            
            // Warning printer goes on top of everything else.
            _warningPrinter = AddComponent<WarningPrinter>();
            _devConsole = AddComponent<DeveloperConsole>();
        }
        
        public void Unload()
        {
            _app.Logger.Log($"Scene is now unloading ({GetType().FullName})");
            while (_components.Any())
                RemoveComponent(_components.First());

            _app.Logger.Log("OnUnload reached.");
            OnUnload();
            _gameLoop = null;
            _app = null;
        }

        internal void Update(GameTime gameTime)
        {
            OnUpdate(gameTime);
            
            foreach (var component in _components.ToArray())
            {
                component.Update(gameTime);
            }
        }

        public void Draw(GameTime gameTime)
        {
            if (Camera != null) 
            {
                var renderer = new Renderer(Game.White, Game.SpriteBatch, Camera);

                foreach (var component in _components)
                    component.Draw(gameTime, renderer);
            }
            else
            {
                Game.SpriteBatch.Begin();

                var text = "No Camera Active on Current Scene";
                var m = _debugFont.MeasureString(text);
                var loc = new Vector2(0.5f, 0.5f) * new Vector2(Game.ScreenWidth, Game.ScreenHeight) - (m / 2);

                Game.SpriteBatch.DrawString(_debugFont, text, loc, Color.White);
                
                Game.SpriteBatch.End();
            }
        }
        
        
        protected virtual void OnUpdate(GameTime gameTime) {}
        protected virtual void OnLoad() {}
        protected virtual void OnUnload() {}

        #region  CordinateHelpers

        public Vector2 ViewportToScreen(Vector2 coordinates)
        {
            var scale = _gameLoop.GraphicsDevice.Viewport.Bounds.Size.ToVector2() / ViewportBounds.Size.ToVector2();
            return coordinates * scale;
        }
        
        public Vector2 ScreenToViewport(Vector2 coordinates)
        {
            var scale = ViewportBounds.Size.ToVector2() / _gameLoop.GraphicsDevice.Viewport.Bounds.Size.ToVector2();
            return coordinates * scale;
        }


        #endregion
    }
}