using System.IO;
using System.Numerics;
using Thundershock.Config;
using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock
{
    public abstract class GraphicalAppBase : AppBase
    {
        private bool _borderless = false;
        private bool _fullscreen = false;
        private int _width;
        private int _height;
        private GameWindow _gameWindow;
        private bool _aboutToExit = false;
        private Renderer2D _renderer;
        private Font _font;
        
        public bool SwapMouseButtons
        {
            get => _gameWindow.PrimaryMouseButtonIsRightMouseButton;
            set => _gameWindow.PrimaryMouseButtonIsRightMouseButton = value;
        }
        
        public bool IsBorderless
        {
            get => _borderless;
            protected set => _borderless = value;
        }

        public bool IsFullScreen
        {
            get => _fullscreen;
            protected set => _fullscreen = value;
        }

        public int ScreenWidth => _width;
        public int ScreenHeight => _height;
        
        protected sealed override void Bootstrap()
        {
            Logger.Log("Creating the game window...");
            _gameWindow = CreateGameWindow();
            _gameWindow.Show(this);
            Logger.Log("Game window created.");

            PreInit();

            _renderer = new Renderer2D(_gameWindow.GraphicsProcessor);
            
            RunLoop();

            Logger.Log("RunLoop just returned. That means we're about to die.");
            _gameWindow.Close();
            _gameWindow = null;
            Logger.Log("Game window destroyed.");
        }

        private void RunLoop()
        {
            var projection = Matrix4x4.Identity;

            var stream = Stream.Null;
            Resource.GetStream(typeof(GraphicalAppBase).Assembly, "Thundershock.Resources.Dick0.png", out stream);
            var texture = Texture2D.FromStream(_gameWindow.GraphicsProcessor, stream);

            stream.Dispose();

            var fontStream = Stream.Null;
            Resource.GetStream(typeof(GraphicalAppBase).Assembly, "Thundershock.Resources.FallbackFont.ttf",
                out fontStream);
            _font = Font.FromTtfStream(_gameWindow.GraphicsProcessor, fontStream);
            
            while (!_aboutToExit)
            {
                projection = Matrix4x4.CreateOrthographicOffCenter(0, _gameWindow.Width, _gameWindow.Height, 0, -1, 1);
                
                _gameWindow.GraphicsProcessor.Clear(Color.Black);

                var peace = new Color(0x1B, 0xAA, 0xF7, 0xFF);
                _renderer.Begin(projection);

                _renderer.FillRectangle(new Rectangle(200, 200, 512, 512), Color.White, texture);
                
                _renderer.DrawString(_font, "Hello world!", Vector2.Zero, Color.White);
                
                _renderer.End();
                
                
                _gameWindow.Update();
            }
        }

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
            Logger.Log("PreInit reached. Setting up core components.");
            RegisterComponent<ConfigurationManager>();

            OnPreInit();
        }

        protected void ApplyGraphicsChanges()
        {
            _gameWindow.IsBorderless = _borderless;
            _gameWindow.IsFullScreen = _fullscreen;
            
            // TODO: V-Sync, Fixed Time Stepping, Monitor Positioning
            _gameWindow.Width = _width;
            _gameWindow.Height = _height;
        }

        protected void SetScreenSize(int width, int height, bool apply = false)
        {
            _width = width;
            _height = height;
            
            if (apply) ApplyGraphicsChanges();
        }
        
        protected virtual void OnPreInit() {}
        
        protected abstract GameWindow CreateGameWindow();
    }
}