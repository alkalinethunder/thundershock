using System;

namespace Thundershock.Core
{
    public abstract class GlobalComponent : IGlobalComponent
    {
        public AppBase App { get; private set; }
        
        public void Initialize(AppBase app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            OnLoad();
        }

        public void Unload()
        {
            OnUnload();
            App = null;
        }

        public void Update(Thundershock.Core.GameTime gameTime)
        {
            OnUpdate(gameTime);
        }
        
        protected virtual void OnLoad() {}
        protected virtual void OnUnload() {}
        
        protected virtual void OnUpdate(Thundershock.Core.GameTime gameTime) {}
        
    }
}