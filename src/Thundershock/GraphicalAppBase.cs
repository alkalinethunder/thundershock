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
        private Texture2D _dickShortForRichie;

        private Vertex[] _vertices = new Vertex[4];
        private int[] _indices = new int[6];
        
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

            _vertices[0].Position = new Vector3(-0.5f, -0.5f, 0);
            _vertices[1].Position = new Vector3(0.5f, -0.5f, 0);
            _vertices[2].Position = new Vector3(-0.5f, 0.5f, 0);
            _vertices[3].Position = new Vector3(0.5f, 0.5f, 0);

            _vertices[0].Color = new Vector4(1, 1, 1, 1);
            _vertices[1].Color = new Vector4(1, 1, 1, 1);
            _vertices[2].Color = new Vector4(1, 1, 1, 1);
            _vertices[3].Color = new Vector4(1, 1, 1, 1);

            _vertices[0].TextureCoordinates = new Vector2(0, 0);
            _vertices[1].TextureCoordinates = new Vector2(1, 0);
            _vertices[2].TextureCoordinates = new Vector2(0, 1);
            _vertices[3].TextureCoordinates = new Vector2(1, 1);
            
            
            _indices[0] = 0;
            _indices[1] = 1;
            _indices[2] = 2;
            _indices[3] = 1;
            _indices[4] = 2;
            _indices[5] = 3;

            if (Resource.GetStream(typeof(GraphicalAppBase).Assembly, "Thundershock.Resources.Dick0.png", out var stream))
            {
                _dickShortForRichie = Texture2D.FromStream(_gameWindow.GraphicsProcessor, stream);
                stream.Dispose();
            }
            
            RunLoop();

            Logger.Log("RunLoop just returned. That means we're about to die.");
            _gameWindow.Close();
            _gameWindow = null;
            Logger.Log("Game window destroyed.");
        }

        private void RunLoop()
        {
            while (!_aboutToExit)
            {
                _gameWindow.Renderer.Clear();

                _gameWindow.Renderer.Textures[0] = _dickShortForRichie;
                
                _gameWindow.Renderer.Begin();
                _gameWindow.Renderer.Draw(PrimitiveType.TriangleStrip, _vertices, _indices, 0, 2);
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