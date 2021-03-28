using System;
using Microsoft.Xna.Framework;

namespace Thundershock
{
    public abstract class GlobalComponent
    {
        protected App App { get; private set; }
        
        public void Initialize(App app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            OnLoad();
        }

        public void Unload()
        {
            OnUnload();
            App = null;
        }

        public void Update(GameTime gameTime)
        {
            OnUpdate(gameTime);
        }
        
        protected virtual void OnLoad() {}
        protected virtual void OnUnload() {}
        
        protected virtual void OnUpdate(GameTime gameTime) {}
        
    }
}