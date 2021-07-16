using Thundershock.Core;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;

namespace Thundershock
{
    /// <summary>
    /// Represents the bottom-most layer in a Thundershock game. Provides code for managing
    /// the current scene and passing input events through to the scene's objects and systems.
    /// </summary>
    public sealed class GameLayer : Layer
    {
        private Scene _currentScene;
        
        /// <summary>
        /// Gets an instance of the game window.
        /// </summary>
        public GameWindow Window => App.Window;

        /// <summary>
        /// Gets an instance of the engine's graphics processor.
        /// </summary>
        public GraphicsProcessor Graphics => Window.GraphicsProcessor;

        /// <summary>
        /// Gets a rectangle representing the bounds of the screen.
        /// </summary>
        public Rectangle ViewportBounds
            => new Rectangle(0, 0, Window.Width, Window.Height);
        
        /// <inheritdoc />
        protected override void OnRender(GameTime gameTime)
        {
            if (_currentScene != null)
                _currentScene.Draw(gameTime);
        }

        /// <inheritdoc />
        protected override void OnInit()
        {
        }

        /// <inheritdoc />
        protected override void OnUnload()
        {
            if (_currentScene != null)
                _currentScene.Unload();

            _currentScene = null;
        }

        /// <inheritdoc />
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
        
        /// <summary>
        /// Loads a new scene.
        /// </summary>
        /// <typeparam name="T">The type of scene to load.</typeparam>
        public void LoadScene<T>() where T : Scene, new()
        {
            var scene = new T();
            LoadSceneInternal(scene);
        }

        /// <summary>
        /// Retrieves an instance of the given app component.
        /// </summary>
        /// <typeparam name="T">The type of app component to find.</typeparam>
        /// <returns>An instance of the given app component type, or null if none was found.</returns>
        public T GetComponent<T>() where T : IGlobalComponent, new()
            => App.GetComponent<T>();

        /// <summary>
        /// Exits the Thundershock engine.
        /// </summary>
        public void Exit() => App.Exit();

        /// <inheritdoc />
        public override bool KeyDown(KeyEventArgs e)
        {
            return _currentScene?.KeyDown(e) ?? false;
        }

        /// <inheritdoc />
        public override bool KeyUp(KeyEventArgs e)
        {
            return _currentScene?.KeyUp(e) ?? false;
        }

        /// <inheritdoc />
        public override bool KeyChar(KeyCharEventArgs e)
        {
            return _currentScene?.KeyChar(e) ?? false;
        }

        /// <inheritdoc />
        public override bool MouseMove(MouseMoveEventArgs e)
        {
            return _currentScene?.MouseMove(e) ?? false;
        }

        /// <inheritdoc />
        public override bool MouseDown(MouseButtonEventArgs e)
        {
            return _currentScene?.MouseDown(e) ?? false;
        }

        /// <inheritdoc />
        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return _currentScene?.MouseUp(e) ?? false;
        }

        /// <inheritdoc />
        public override bool MouseScroll(MouseScrollEventArgs e)
        {
            return _currentScene?.MouseScroll(e) ?? false;
        }
    }
}