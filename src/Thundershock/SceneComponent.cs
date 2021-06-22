using System;
using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock
{
    [Obsolete("Good fucking God stop using this and use the damn ECS.")]
    public abstract class SceneComponent
    {
        private Scene _scene;

        public Scene Scene => _scene;

        public bool Visible { get; set; } = true;
        
        public void Load(Scene scene)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            
            OnLoad();
        }

        public void Unload()
        {
            OnUnload();
            _scene = null;
        }

        public void Update(GameTime gameTime)
        {
            OnUpdate(gameTime);
        }

        public void Draw(GameTime gameTime, Renderer2D renderer)
        {
            if (Visible)
            {
                OnDraw(gameTime, renderer);
            }
        }

        protected virtual void OnLoad() {}
        protected virtual void OnUnload() {}

        protected virtual void OnUpdate(GameTime gameTime) {}
        protected virtual void OnDraw(GameTime gameTime, Renderer2D renderer) {}

    }
}
