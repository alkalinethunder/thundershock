using System;
using System.Diagnostics;
using Thundershock.Audio;
using Thundershock.Config;
using Thundershock.Core;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;
using Thundershock.Debugging;

namespace Thundershock
{
    /// <summary>
    /// Provides the base functionality for all graphics-capable Thundershock applications.
    /// </summary>
    public abstract class GraphicalAppBase : AppBase
    {
        private bool _borderless;
        private bool _fullscreen;
        private int _width;
        private int _height;
        private GameWindow _gameWindow;
        private bool _aboutToExit;
        private Stopwatch _frameTimer = new();
        private TimeSpan _totalGameTime;
        private LayerManager _layerManager;
        private GameLayer _gameLayer;
        private MusicPlayer _musicPlayer;

        /// <summary>
        /// Gets an instance of the graphics processor.
        /// </summary>
        public GraphicsProcessor Graphics => Window.GraphicsProcessor;
        
        /// <summary>
        /// Gets an instance of the game window.
        /// </summary>
        public GameWindow Window => _gameWindow;
        
        /// <summary>
        /// Gets an instance of the engine's layer manager.
        /// </summary>
        public LayerManager LayerManager => _layerManager;
        
        /// <summary>
        /// Gets or sets a value indicating whether the primary mouse button is the left or right mouse button.
        /// </summary>
        public bool SwapMouseButtons
        {
            get => _gameWindow.PrimaryMouseButtonIsRightMouseButton;
            set => _gameWindow.PrimaryMouseButtonIsRightMouseButton = value;
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the game window has window decorations.
        /// </summary>
        public bool IsBorderless
        {
            get => _borderless;
            protected set => _borderless = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the game window is full-screen/maximizedd.
        /// </summary>
        public bool IsFullScreen
        {
            get => _fullscreen;
            protected set => _fullscreen = value;
        }

        /// <summary>
        /// Gets the current screen width.
        /// </summary>
        public int ScreenWidth => _width;
        
        /// <summary>
        /// Gets the current screen height.
        /// </summary>
        public int ScreenHeight => _height;
        
        /// <inheritdoc />
        protected sealed override void Bootstrap()
        {
            Logger.Log("Creating the game window...");
            _gameWindow = CreateGameWindow();
            _gameWindow.Show(this);
            Logger.Log("Game window created.");

            PreInit();
            
            RunLoop();

            Logger.Log("RunLoop just returned. That means we're about to die.");
            _gameWindow.Close();
            _gameWindow = null;
            Logger.Log("Game window destroyed.");
        }

        private void RunLoop()
        {
            Init();
            PostInit();
            
            while (!_aboutToExit)
            {
                var gameTime = _frameTimer.Elapsed;
                var frameTime = gameTime - _totalGameTime;
                var gameTimeInfo = new GameTime(frameTime, gameTime);
                
                // Run enqueued actions.
                RunQueuedActions();
                
                // Tick the music player
                _musicPlayer.Update(frameTime.TotalSeconds);
                
                // Tick all of the global app modules.
                UpdateComponents(gameTimeInfo);

                _layerManager.Update(gameTimeInfo);
                
                _gameWindow.GraphicsProcessor.Clear(Color.Black);
                
                _layerManager.Render(gameTimeInfo);
                
                _gameWindow.Update();
                
                _totalGameTime = gameTime;
            }
        }

        /// <inheritdoc />
        protected override void BeforeExit(AppExitEventArgs args)
        {
            // call the base method to dispatch the event to the rest of the engine.
            base.BeforeExit(args);
            
            // Terminate the game loop if args.Cancelled isn't set
            if (!args.CancelExit)
            {
                _aboutToExit = true;
            }
        }

        private void PreInit()
        {
            _layerManager = new LayerManager(this);
            
            Logger.Log("PreInit reached. Setting up core components.");
            RegisterComponent<ConfigurationManager>();

            // Bind input events to the layer manager.
            _gameWindow.KeyDown += GameWindowOnKeyDown;
            _gameWindow.KeyUp += GameWindowOnKeyUp;
            _gameWindow.KeyChar += GameWindowOnKeyChar;
            
            _gameWindow.MouseDown += GameWindowOnMouseDown;
            _gameWindow.MouseUp += GameWindowOnMouseUp;
            _gameWindow.MouseMove += GameWindowOnMouseMove;
            _gameWindow.MouseScroll += GameWindowOnMouseScroll;
            OnPreInit();
        }

        private void GameWindowOnMouseScroll(object sender, MouseScrollEventArgs e)
        {
            _layerManager.FireMouseScroll(e);
        }

        private void GameWindowOnMouseMove(object sender, MouseMoveEventArgs e)
        {
            _layerManager.FireMouseMove(e);
        }

        private void GameWindowOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _layerManager.FireMouseUp(e);
        }

        private void GameWindowOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _layerManager.FireMouseDown(e);
        }

        private void GameWindowOnKeyChar(object sender, KeyCharEventArgs e)
        {
            _layerManager.FireKeyChar(e);
        }

        private void GameWindowOnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.BackQuote)
            {
                if (!_layerManager.HasLayer<DevConsole>())
                {
                    var tcmd = new DevConsole();
                    _layerManager.PushOverlay(tcmd);
                    return;
                }
            }
            
            _layerManager.FireKeyUp(e);
        }

        private void GameWindowOnKeyDown(object sender, KeyEventArgs e)
        {
            _layerManager.FireKeyDown(e);
        }

        private void Init()
        {
            RegisterComponent<CheatManager>();

            _gameLayer = new GameLayer();
            _layerManager.PushLayer(_gameLayer);
            
            _layerManager.PushOverlay(new FpsCountLayer());
            
            OnInit();
        }

        private void PostInit()
        {
            _musicPlayer = MusicPlayer.GetInstance();
            
            OnPostInit();

            _frameTimer.Start();
        }

        /// <summary>
        /// Applies the current application settings to the game window.
        /// </summary>
        protected void ApplyGraphicsChanges()
        {
            _gameWindow.IsBorderless = _borderless;
            _gameWindow.IsFullScreen = _fullscreen;
            
            // TODO: V-Sync, Fixed Time Stepping, Monitor Positioning
            _gameWindow.Width = _width;
            _gameWindow.Height = _height;
        }

        /// <summary>
        /// Sets the desired screen size.
        /// </summary>
        /// <param name="width">The desired screen width.</param>
        /// <param name="height">The desired screen height.</param>
        /// <param name="apply">Whether the given settings should be immediately applied.</param>
        protected void SetScreenSize(int width, int height, bool apply = false)
        {
            _width = width;
            _height = height;
            
            if (apply) ApplyGraphicsChanges();
        }
        
        /// <summary>
        /// Called before the application initializes the game framework.
        /// </summary>
        protected virtual void OnPreInit() {}
        
        /// <summary>
        /// Called during initialization of the game framework.
        /// </summary>
        protected virtual void OnInit() {}
        
        /// <summary>
        /// Called after the engine has been fully initialized.
        /// </summary>
        protected virtual void OnPostInit() {}

        /// <summary>
        /// When overridden in a derived class, creates the game window.
        /// </summary>
        /// <returns>An instance of the newly created game window.</returns>
        protected abstract GameWindow CreateGameWindow();

        /// <summary>
        /// Loads and activates a new scene.
        /// </summary>
        /// <typeparam name="T">The type of scene to load.</typeparam>
        protected void LoadScene<T>() where T : Scene, new()
        {
            _gameLayer.LoadScene<T>();
        }
    }
}