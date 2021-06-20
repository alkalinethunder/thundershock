using SDL2;
using Thundershock.Core;
using Thundershock.Core.Audio;
using Thundershock.Core.Rendering;

namespace Thundershock
{
    internal sealed class GameLoop
    {
        private GraphicalAppBase _app;
        private GameWindow _gameWindow;
        private Scene _currentScene = null;

        public GraphicalAppBase App => _app;

        public AudioBackend Audio => _gameWindow.AudioBackend;
        public GraphicsProcessor Graphics => _gameWindow.GraphicsProcessor;

        public Rectangle ViewportBounds
            => new Rectangle(0, 0, _gameWindow.Width, _gameWindow.Height);
        
        public GameLoop(GraphicalAppBase app, GameWindow gameWindow)
        {
            _app = app;
            _gameWindow = gameWindow;
        }

        public void Update(GameTime gameTime)
        {
            if (_currentScene != null)
            {
                _currentScene.Update(gameTime);
            }
        }

        public void Render(GameTime gameTime)
        {
            if (_currentScene != null)
                _currentScene.Draw(gameTime);
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
        
        
    }
}