using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Config;
using Thundershock.Debugging;
using Thundershock.Input;

namespace Thundershock
{
    public class MonoGameLoop : Game
    {
        private GameAppBase _app;
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

        public bool VSync
        {
            get => _graphics.SynchronizeWithVerticalRetrace;
            set => _graphics.SynchronizeWithVerticalRetrace = value;
        }

        public bool IsFullScreen
        {
            get => _graphics.IsFullScreen;
            set => _graphics.IsFullScreen = value;
        }

        public bool EnableBloom
        {
            get => _postProcessor.EnableBloom;
            set => _postProcessor.EnableBloom = value;
        }

        public bool EnableShadowmask
        {
            get => _postProcessor.EnableShadowMask;
            set => _postProcessor.EnableShadowMask = value;
        }

        public bool AllowPostProcessing { get; set; } = false;

        internal MonoGameLoop(GameAppBase app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _app.Logger.Log("Bootstrapping MonoGame...");
            _graphics = new GraphicsDeviceManager(this);
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _thundershockContent = new ContentManager(this.Services);
            _thundershockContent.RootDirectory = "ThundershockContent";
        }

        private void LoadScene(Scene scene)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            if (_activeScene != null)
                _activeScene.Unload();

            _activeScene = scene;
            scene.Load(_app, this);
        }

        internal void LoadScene<T>() where T : Scene, new()
        {
            var scene = new T();
            LoadScene(scene);
        }

        private void AllocateRenderTarget()
        {
            // clean up the old render target.
            if (_renderTarget != null)
            {
                _app.Logger.Log("Disposing the old game render target...");
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            // re-allocate the render target
            _renderTarget = new RenderTarget2D(GraphicsDevice, ScreenWidth, ScreenHeight, false,
                GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None, 0,
                RenderTargetUsage.PreserveContents);
            _app.Logger.Log($"Allocated game render target ({_renderTarget.Width}x{_renderTarget.Height})");

            // re-allocate post-process effect buffers
            _app.Logger.Log("Telling the post-processor to re-allocate effect buffers.");
            _postProcessor.ReallocateEffectBuffers();
        }

        public void ApplyDisplayChanges()
        {
            _app.Logger.Log("Graphics mode has been changed in the config. Applying these changes.");

            // apply the changes in MonoGame
            _graphics.ApplyChanges();

            // re-allocate the render target
            this.AllocateRenderTarget();

            _app.Logger.Log("Done.");
        }

        public void SetScreenSize(int width, int height, bool apply = false)
        {
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;

            if (apply) ApplyDisplayChanges();
        }


        protected override void Initialize()
        {
            _app.Logger.Log("Initializing MonoGame...");

            // Initialize the app. This officially completes the gluing of Thundershock to MonoGame.
            // It also gives the game a chance to do pre-graphics initialization.
            _app.Initialize();

            // HACK: I don't like that we need to do this. But whatever.
            _white = new Texture2D(GraphicsDevice, 1, 1);
            _white.SetData<uint>(new[] {0xFFFFFFFF});

            // Initialize the post-processor.
            _postProcessor = new PostProcessor(GraphicsDevice);

            // Set up the minimal configuration.
            VSync = false;
            IsFullScreen = false;
            IsFixedTimeStep = true;
            AllowPostProcessing = false;
            EnableBloom = false;
            EnableShadowmask = false;
            SetScreenSize(1024, 768, true);


            base.Initialize();
            _app.Logger.Log("MonoGame initialized successfully.");
        }

        protected override void LoadContent()
        {
            // Create our SpriteBatch object.
            _app.Logger.Log("Setting up the SpriteBatch...");
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the shaders used by the post-processor.
            _app.Logger.Log("Loading shaders for post-processor...");
            _postProcessor.LoadContent(_thundershockContent);

            // Allow the app to do post-initialization.
            _app.Logger.Log("Engine content ready. Telling the app to load...");
            _app.Load();
        }

        protected override void UnloadContent()
        {
            _app.Logger.Log("MonoGame is tearing us down...");
            if (_activeScene != null)
            {
                _app.Logger.Log("Unloading the current Scene...");
                _activeScene.Unload();
                _activeScene = null;
                _app.Logger.Log("Done.");
            }

            // Allow the app to unload.
            _app.Logger.Log("Telling the app to unload before we tear down gfx resources...");
            _app.Unload();

            // Tear down the post-processor.
            _app.Logger.Log("Telling the post-processor to release its resources...");
            _postProcessor.UnloadContent();

            // de-allocate the hack texture
            _app.Logger.Log("Releasing engine textures...");
            _app.Logger.Log(
                "Jesus H. Fucking Christ, WHY do we need to have an always-loaded 1x1 texture that's just solid white!? Why is this code necessary!?", LogLevel.Trace);
            _white.Dispose();

            // de-allocate the game render target
            _renderTarget.Dispose();

            _app.Logger.Log("Out gfx resources are gone, telling MonoGame to finish tearing down.");
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

        protected override void OnExiting(object sender, EventArgs args)
        {
            _app.Logger.Log("MonoGame is requesting us to exit, we're going to run that by the app first.");

            if (_app.Exit())
            {
                _app.Logger.Log("App had no objections, tearing ourselves down...");
                base.OnExiting(sender, args);
            }
        }
    }
}
