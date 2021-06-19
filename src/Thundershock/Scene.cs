using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Rendering;
using Thundershock.Rendering;

namespace Thundershock
{
    public abstract class Scene
    {
        private GameLoop _gameLoop;
        private List<SceneComponent> _components = new List<SceneComponent>();
        private Font _debugFont;
        private Renderer2D _renderer;

        public Camera Camera { get; protected set; }

        public Rectangle ViewportBounds
            => _gameLoop.ViewportBounds;

        public GraphicalAppBase App => _gameLoop.App;

        public GraphicsProcessor Graphics => _gameLoop.Graphics;
        
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
            _components.Add(component);
            component.Load(this);
        }

        public void GoToScene<T>() where T : Scene, new ()
        {
            _gameLoop.LoadScene<T>();
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

            component.Unload();
            _components.Remove(component);
        }

        internal void Load(GameLoop gameLoop)
        {
            _gameLoop = gameLoop ?? throw new ArgumentNullException(nameof(gameLoop));
            _debugFont = Font.GetDefaultFont(_gameLoop.Graphics);
            _renderer = new Renderer2D(_gameLoop.Graphics);
            OnLoad();
        }

        public void Unload()
        {
            while (_components.Any())
                RemoveComponent(_components.First());

            OnUnload();
            _gameLoop = null;
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
            _renderer.ProjectionMatrix =
                Matrix4x4.CreateOrthographicOffCenter(0, ViewportBounds.Width, ViewportBounds.Height, 0, -1, 1);
            
            if (Camera != null)
            {
                foreach (var component in _components)
                    component.Draw(gameTime, _renderer);
            }
            else
            {
                _renderer.Begin();

                var text = "No Camera Active on Current Scene";
                var m = _debugFont.MeasureString(text);
                var loc = Vector2.Zero;

                _renderer.DrawString(_debugFont, text, loc, Color.White);

                _renderer.End();
            }
        }


        protected virtual void OnUpdate(GameTime gameTime) {}
        protected virtual void OnLoad() {}
        protected virtual void OnUnload() {}

        #region  CordinateHelpers

        public Vector2 ViewportToScreen(Vector2 coordinates)
        {
            return coordinates;
        }

        public Vector2 ScreenToViewport(Vector2 coordinates)
        {
            return coordinates;
        }


        #endregion
    }
}
