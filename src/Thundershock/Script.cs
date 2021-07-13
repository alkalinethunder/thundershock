using Thundershock.Core;
using Thundershock.GameFramework;

namespace Thundershock
{
    public abstract class Script
    {
        private Scene _scene;
        private SceneObject _sceneObject;

        protected Scene Scene => _scene;
        protected SceneObject SceneObject => _sceneObject;

        internal void Init(Scene scene, SceneObject obj)
        {
            _scene = scene;
            _sceneObject = obj;

            OnCreate();
        }
        
        public virtual void OnCreate() {}
        
        public virtual void OnDestroy() {}
        public virtual void OnUpdate(GameTime gameTime) {}
    }
}