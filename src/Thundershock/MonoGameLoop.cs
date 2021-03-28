using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Input;

namespace Thundershock
{
    public class MonoGameLoop : Game
    {
        private static MonoGameLoop _instance;
        
        public static MonoGameLoop Instance
            => _instance;

        private App _app;
        private PostProcessor _postProcessor;
        private RenderTarget2D _renderTarget;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _white;
        private Scene _activeScene;
        private ContentManager _thundershockContent;
        
        public Texture2D White => _white;
        
        public PostProcessor.PostProcessSettings PostProcessSettings => _postProcessor.Settings;
        
        public SpriteBatch SpriteBatch => _spriteBatch;

        public int ScreenWidth
            => GraphicsDevice.PresentationParameters.BackBufferWidth;
        
        public int ScreenHeight
            => GraphicsDevice.PresentationParameters.BackBufferHeight;

        public ContentManager EngineContent
            => _thundershockContent;
        
        internal MonoGameLoop(App app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _instance = this;
            _graphics = new GraphicsDeviceManager(this);
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _thundershockContent = new ContentManager(this.Services);
            _thundershockContent.RootDirectory = "ThundershockContent";
        }
        
        public void LoadScene(Scene scene)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            if (_activeScene != null)
                _activeScene.Unload();

            _activeScene = scene;
            scene.Load(_app, this);
        }
        
        public void LoadScene<T>() where T : Scene, new()
        {
            var scene = new T();
            LoadScene(scene);
        }

        private void AllocateRenderTarget()
        {
            // clean up the old render target.
            if (_renderTarget != null)
            {
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            // re-allocate the render target
            _renderTarget = new RenderTarget2D(GraphicsDevice, ScreenWidth, ScreenHeight, false,
                GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None, 0,
                RenderTargetUsage.PreserveContents);
            
            // re-allocate post-process effect buffers
            _postProcessor.ReallocateEffectBuffers();
        }
        
        protected override void Initialize()
        {
            // HACK: I don't like that we need to do this. But whatever.
            _white = new Texture2D(GraphicsDevice, 1, 1);
            _white.SetData<uint>(new[] {0xFFFFFFFF});
            
            // Initialize the post-processor.
            // TODO: Honour the --no-postprocessor flag.
            _postProcessor = new PostProcessor(GraphicsDevice);

            // Allocate the game render target.
            AllocateRenderTarget();

            // Initialize the app. This officially completes the gluing of Thundershock to MonoGame.
            // It also gives the game a chance to do pre-graphics initialization.
            _app.Initialize(this);
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create our SpriteBatch object.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the shaders used by the post-processor.
            _postProcessor.LoadContent(_thundershockContent);
            
            // Allow the app to do post-initialization.
            _app.Load();
        }

        protected override void UnloadContent()
        {
            if (_activeScene != null)
            {
                _activeScene.Unload();
                _activeScene = null;
            }

            // Allow the app to unload.
            _app.Unload();
            
            // Tear down the post-processor.
            _postProcessor.UnloadContent();
            
            // de-allocate the hack texture
            _white.Dispose();

            // de-allocate the game render target
            _renderTarget.Dispose();
            
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Allow the app to update
            _app.Update(gameTime);
            
            _activeScene?.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_renderTarget);
            GraphicsDevice.Clear(Color.Black);

            _activeScene?.Draw(gameTime);

            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            _postProcessor.Process(_renderTarget);
            
            base.Draw(gameTime);
        }
    }
}
