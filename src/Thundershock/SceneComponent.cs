using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock
{
    public abstract class SceneComponent
    {
        private Scene _scene;

        public App App => _scene.App;
        public Scene Scene => _scene;

        public bool Visible { get; set; } = true;
        
        public MonoGameLoop Game => _scene.Game;
        
        public void Load(Scene scene)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            App.Logger.Log("OnLoad reached.");
            OnLoad();
        }

        public void Unload()
        {
            App.Logger.Log("OnUnload reached.");
            OnUnload();
            _scene = null;
        }

        public void Update(GameTime gameTime)
        {
            OnUpdate(gameTime);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (Visible)
            {
                OnDraw(gameTime, spriteBatch);
            }
        }
        
        protected virtual void OnLoad() {}
        protected virtual void OnUnload() {}

        protected virtual void OnUpdate(GameTime gameTime) {}
        protected virtual void OnDraw(GameTime gameTime, SpriteBatch batch) {}

    }
}