using Thundershock.Core;
using Thundershock.Core.Debugging;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;

namespace Thundershock
{
    /// <summary>
    /// Represents the bottom-most layer in a Thundershock game. Provides code for managing
    /// the current scene and passing input events through to the scene's objects and systems.
    /// </summary>
    [CheatAlias("Game")]
    public sealed class GameLayer : Layer
    {
        private Scene _currentScene;

        /// <summary>
        /// Gets or sets a value indicating whether the engine will override the viewport rectangle using
        /// <see cref="ViewportOverrideBounds"/>. This is off by default and is mainly used by the editor.
        /// </summary>
        public bool OverrideViewport { get; set; }
        
        /// <summary>
        /// Gets or sets a rectangle representing the overridden viewport bounds.
        /// </summary>
        public Rectangle ViewportOverrideBounds { get; set; }
        
        /// <summary>
        /// Gets an instance of the game window.
        /// </summary>
        public GameWindow Window => App.Window;

        /// <summary>
        /// Gets an instance of the engine's graphics processor.
        /// </summary>
        public GraphicsProcessor Graphics => GamePlatform.GraphicsProcessor;

        /// <summary>
        /// Gets a rectangle representing the bounds of the screen.
        /// </summary>
        public Rectangle ViewportBounds
            => OverrideViewport ? ViewportOverrideBounds :
                new(0, 0, Window.Width, Window.Height);
        
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
        public T LoadScene<T>() where T : Scene, new()
        {
            var scene = new T();
            LoadSceneInternal(scene);
            return scene;
        }
        
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