using SDL2;
using Thundershock.Core;
using Thundershock.Core.Audio;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;
using Thundershock.Debugging;

namespace Thundershock
{
    public sealed class GameLayer : Layer
    {
        private Scene _currentScene = null;

        public GameWindow Window => App.Window;
        
        public AudioBackend Audio => Window.AudioBackend;
        public GraphicsProcessor Graphics => Window.GraphicsProcessor;

        public Rectangle ViewportBounds
            => new Rectangle(0, 0, Window.Width, Window.Height);
        
        protected override void OnRender(GameTime gameTime)
        {
            if (_currentScene != null)
                _currentScene.Draw(gameTime);
        }

        protected override void OnInit()
        {
        }

        protected override void OnUnload()
        {
            if (_currentScene != null)
                _currentScene.Unload();

            _currentScene = null;
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            if (_currentScene != null)
            {
                _currentScene.Update(gameTime);
            }
        }

        private void LoadSceneInternal(Scene scene)
        {
            if (_currentScene != null)
            {
                UnloadSceneInternal();
            }

            scene.Load(this);
            
            _currentScene = scene;
        }

        private void UnloadSceneInternal()
        {
            if (_currentScene != null)
            {
                _currentScene.Unload();
                _currentScene = null;
            }
        }
        
        public void LoadScene<T>() where T : Scene, new()
        {
            var scene = new T();
            LoadSceneInternal(scene);
        }

        public T GetComponent<T>() where T : IGlobalComponent, new()
            => App.GetComponent<T>();

        public void Exit() => App.Exit();

        public override bool KeyDown(KeyEventArgs e)
        {
            return _currentScene.KeyDown(e);
        }

        public override bool KeyUp(KeyEventArgs e)
        {
            return _currentScene.KeyUp(e);
        }

        public override bool KeyChar(KeyCharEventArgs e)
        {
            return _currentScene.KeyChar(e);
        }

        public override bool MouseMove(MouseMoveEventArgs e)
        {
            return _currentScene.MouseMove(e);
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            return _currentScene.MouseDown(e);
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return _currentScene.MouseUp(e);
        }

        public override bool MouseScroll(MouseScrollEventArgs e)
        {
            return _currentScene.MouseScroll(e);
        }
    }
}