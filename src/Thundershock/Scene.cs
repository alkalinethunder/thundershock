using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Thundershock
{
    public abstract class Scene
    {
        private App _app;
        private MonoGameLoop _gameLoop;
        private List<SceneComponent> _components = new List<SceneComponent>();

        public App App => _app;
        public MonoGameLoop Game => _gameLoop;

        public void AddComponent(SceneComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));
            if (component.Scene != null)
                throw new InvalidOperationException("Scene component already belongs to a Scene.");
            _components.Add(component);
            component.Load(this);
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
        
        internal void Load(App app, MonoGameLoop gameLoop)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _gameLoop = gameLoop ?? throw new ArgumentNullException(nameof(gameLoop));
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
            foreach (var component in _components)
            {
                component.Draw(gameTime, _gameLoop.SpriteBatch);
            }
        }
        
        
        protected virtual void OnUpdate(GameTime gameTime) {}
        protected virtual void OnLoad() {}
        protected virtual void OnUnload() {}
    }
}