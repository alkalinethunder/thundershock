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
        private RenderTarget2D _renderTarget;
        
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

            _renderTarget = new RenderTarget2D(_gameWindow.GraphicsProcessor, 3840, 2160);
            
            RunLoop();

            Logger.Log("RunLoop just returned. That means we're about to die.");
            _gameWindow.Close();
            _gameWindow = null;
            Logger.Log("Game window destroyed.");
        }

        private void RunLoop()
        {
            var vs = new Vertex[4];
            var indices = new int[6];

            vs[0].Color = Color.White.ToVector4();
            vs[1].Color = Color.White.ToVector4();
            vs[2].Color = Color.White.ToVector4();
            vs[3].Color = Color.White.ToVector4();

            vs[0].Position = new Vector3(-1, -1, 0);
            vs[1].Position = new Vector3(1, -1, 0);
            vs[2].Position = new Vector3(-1, 1, 0);
            vs[3].Position = new Vector3(1, 1, 0);

            vs[0].TextureCoordinates = new Vector2(0, 0);
            vs[1].TextureCoordinates = new Vector2(1, 0);
            vs[2].TextureCoordinates = new Vector2(0, 1);
            vs[3].TextureCoordinates = new Vector2(1, 1);

            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;
            indices[3] = 1;
            indices[4] = 2;
            indices[5] = 3;

            while (!_aboutToExit)
            {
                _gameWindow.Renderer.SetRenderTarget(_renderTarget);
                _gameWindow.Renderer.Clear(new Color(0xf7, 0x1b, 0x1b));

                _gameWindow.Renderer.SetRenderTarget(null);
                _gameWindow.Renderer.Clear();

                _gameWindow.Renderer.Textures[0] = _renderTarget;
                _gameWindow.Renderer.Begin(Matrix4x4.Identity);
                _gameWindow.Renderer.Draw(PrimitiveType.TriangleStrip, vs, indices, 0, 2);
                _gameWindow.Renderer.End();

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