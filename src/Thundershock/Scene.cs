using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Thundershock.Components;
using Thundershock.Core;
using Thundershock.Core.Ecs;
using Thundershock.Core.Debugging;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;
using Thundershock.Debugging;
using Thundershock.GameFramework;
using Thundershock.Gui;
using Thundershock.Gui.Elements;
using Thundershock.Input;
using Thundershock.Rendering;

namespace Thundershock
{
    /// <summary>
    /// Provides functionality for the creation of a game world.
    /// </summary>
    [CheatAlias("Scene")]
    public abstract class Scene
    {
        private const uint MaxEntityCount = 10000;

        private List<ISystem> _systems = new();
        private bool _paused;
        private bool _noClip;
        private CameraManager _cameraManager;
        private GameLayer _gameLoop;
        private Renderer2D _renderer;
        private InputSystem _input = new();
        private Registry _registry;
        private Font _deathFont;
        private GuiSystem _sceneGui;
        private PostProcessor _postProcessSystem;
        private RenderTarget2D _sceneRenderTarget;
        
        /// <summary>
        /// Gets an instance of the all-seeing, all-knowing <see cref="GameLayer"/> that manages this scene.
        /// </summary>
        public GameLayer Game => _gameLoop;
        
        /// <summary>
        /// Gets an instance of this scene's input system.
        /// </summary>
        public InputSystem InputSystem => _input;

        /// <summary>
        /// Gets a rectangle value indicating the bounds of the game's screen.
        /// </summary>
        public Rectangle Screen => new Rectangle(
            0,
            0,
            _gameLoop.Window.Width,
            _gameLoop.Window.Height
        );

        private Camera Camera => _cameraManager.ActiveCamera;
        
        /// <summary>
        /// Gets an instance of the GUI system for this scene.
        /// </summary>
        public GuiSystem Gui => _sceneGui;

        /// <summary>
        /// Gets a rectangle value indicating the game's viewport bounds.
        /// </summary>
        public Rectangle ViewportBounds
            => _gameLoop.ViewportBounds;
        
        /// <summary>
        /// Gets an instance of the game's graphics processor.
        /// </summary>
        public GraphicsProcessor Graphics => _gameLoop.Graphics;

        /// <summary>
        /// Gets an instance of the game's primary camera.
        /// </summary>
        protected CameraComponent PrimaryCameraSettings => PrimaryCamera.GetComponent<CameraComponent>();
        
        /// <summary>
        /// Creates a new instance of the Scene class.
        /// </summary>
        protected Scene()
        {
            // Create the camera manager for this scene.
            _cameraManager = new(this);
            _registry = new Registry(MaxEntityCount);
        }
        
        /// <summary>
        /// Loads a new scene.
        /// </summary>
        /// <typeparam name="T">The type of scene to load.</typeparam>
        public void GoToScene<T>() where T : Scene, new ()
        {
            _gameLoop.LoadScene<T>();
        }
        
        internal void Load(GameLayer gameLoop)
        {
            _deathFont = Font.GetDefaultFont(gameLoop.Graphics);
            _renderer = new Renderer2D(gameLoop.Graphics);
            _gameLoop = gameLoop;
            Font.GetDefaultFont(_gameLoop.Graphics);
            _renderer = new Renderer2D(_gameLoop.Graphics);
            _sceneGui = new GuiSystem(_gameLoop.Graphics);
            
            // Enable and load the post-process system IF the engine's command-line arguments say we should.
            if (!EntryPoint.GetBoolean(EntryBoolean.DisablePostProcessing))
            {
                _postProcessSystem = new PostProcessor(_gameLoop.Graphics);
                _postProcessSystem.LoadContent();
            }
            
            // Help a playa out, spawn the default camera entity.
            var cam = SpawnObject();
            cam.AddComponent(new CameraComponent());
            cam.AddComponent(new Transform());
            
            // Ensure that the scene render target matches the viewport size.
            EnsureSceneRenderTargetSize();

            _gameLoop.GetComponent<CheatManager>().AddObject(this);

            // Add common scene cheats.
            _gameLoop.GetComponent<CheatManager>().AddObject(_postProcessSystem);
            _gameLoop.GetComponent<CheatManager>().AddObject(_registry);

            OnLoad();
        }

        internal bool MouseDown(MouseButtonEventArgs e)
        {
            if (!_sceneGui.MouseDown(e))
                _input.FireMouseDown(e);
            return true;
        }

        internal bool MouseUp(MouseButtonEventArgs e)
        {
            if (!_sceneGui.MouseUp(e))
                _input.FireMouseUp(e);
            return true;
        }

        internal bool MouseMove(MouseMoveEventArgs e)
        {
            if (_noClip)
            {
                var deltaX = 0.5f * e.DeltaX;
                var deltaY = 0.5f * -e.DeltaY;

                var trans = PrimaryCamera.GetComponent<Transform>();
                
                trans.Rotation.Yaw += deltaX;
                trans.Rotation.Pitch += deltaY;
                
            }
            else
            {
                if (!_sceneGui.MouseMove(e))
                    _input.FireMouseMove(e);
            }

            return true;
        }

        internal bool MouseScroll(MouseScrollEventArgs e)
        {
            if (!_sceneGui.MouseScroll(e))
                _input.FireMouseScroll(e);
            return true;
        }
        
        internal bool KeyChar(KeyCharEventArgs e)
        {
            if (!_sceneGui.KeyChar(e))
                _input.FireKeyChar(e);
            return true;
        }
        
        internal bool KeyUp(KeyEventArgs e)
        {
            if (!_sceneGui.KeyUp(e))
                _input.FireKeyUp(e);
            return true;
        }
        
        internal bool KeyDown(KeyEventArgs e)
        {
            if (_noClip)
            {
                var trans = PrimaryCamera.GetComponent<Transform>();
                var moveSpeed = 0.5f;
                
                switch (e.Key)
                {
                    case Keys.PageUp:
                        trans.Position.Y += moveSpeed;
                        break;
                    case Keys.PageDown:
                        trans.Position.Y -= moveSpeed;
                        break;
                    case Keys.W:
                        trans.Position.Z += moveSpeed;
                        break;
                    case Keys.S:
                        trans.Position.Z -= moveSpeed;
                        break;
                    case Keys.A:
                        trans.Position.X -= moveSpeed;
                        break;
                    case Keys.D:
                        trans.Position.X += moveSpeed;
                        break;
                }

                return true;
            }
            else
            {
                if (!_sceneGui.KeyDown(e))
                    _input.FireKeyDown(e);
                return true;
            }
        }
        
        internal void Unload()
        {
            // Teardown all of the systems.
            while (_systems.Any())
            {
                var sys = _systems.First();
                sys.Unload();
                _systems.Remove(sys);
            }
            
            _gameLoop.GetComponent<CheatManager>().RemoveObject(this);
            
            _gameLoop.GetComponent<CheatManager>().RemoveObject(_postProcessSystem);
            _gameLoop.GetComponent<CheatManager>().RemoveObject(_registry);
            
            OnUnload();
            _sceneRenderTarget.Dispose();
            _postProcessSystem.UnloadContent();
            _gameLoop = null;
            _sceneRenderTarget = null;
        }

        internal void Update(GameTime gameTime)
        {
            // Ensure that the scene render target matches the viewport size.
            EnsureSceneRenderTargetSize();

            // Set the scene GUI viewport to match ours.
            _sceneGui.SetViewportSize(_gameLoop.Window.Width, _gameLoop.Window.Height);

            if (!_paused)
            {
                // Tick all of our registered systems.
                foreach (var system in _systems)
                    system.Update(gameTime);
                
                // Update the scene's GUI
                _sceneGui.Update(gameTime);

                // Update scripts.
                foreach (var entity in _registry.View<ScriptComponent>())
                {
                    var script = _registry.GetComponent<ScriptComponent>(entity);
                    script.Update(gameTime);
                }
                
                OnUpdate(gameTime);
            }
        }

        internal void Draw(GameTime gameTime)
        {
            // Switch the GPU to our scene render target for the first render pass where we
            // draw all scene elements.
            _gameLoop.Graphics.SetRenderTarget(_sceneRenderTarget);
            
            var cameras = _registry.View<CameraComponent, Transform>();
            if (cameras.Any())
            {
                var lastCamera = cameras.Last();

                ref var cameraComponent = ref _registry.GetComponent<CameraComponent>(lastCamera);
                ref var cameraTransform = ref _registry.GetComponent<Transform>(lastCamera);

                Camera.OrthoRight = cameraComponent.OrthoWidth;
                Camera.OrthoBottom = cameraComponent.OrthoHeight;

                Camera.Transform.Position = cameraTransform.Position;
                Camera.Transform.Rotation = cameraTransform.Rotation;
                Camera.Transform.Scale = cameraTransform.Scale;

                Camera.ProjectionType = cameraComponent.ProjectionType;

                _postProcessSystem.SettingsFromCameraComponent(cameraComponent);

                _gameLoop.Graphics.Clear(cameraComponent.SkyColor);
            }
            else
            {
                _gameLoop.Graphics.Clear(Color.Black);
            }

            var renderables2D = _registry.View<Transform2D>();
            foreach (var renderable in renderables2D)
            {
                ref var transform2D = ref _registry.GetComponent<Transform2D>(renderable);

                var transformMatrix = transform2D.GetTransformMatrix();

                Matrix4x4.Invert(transformMatrix, out var mvp);
                mvp = transformMatrix * Camera.ProjectionMatrix;

                _renderer.ProjectionMatrix = mvp;

                var sprite = default(Sprite);
                if (_registry.TryGetComponent(renderable, ref sprite))
                {
                    var rect = new Rectangle(0, 0, sprite.Size.X, sprite.Size.Y);
                    rect.X = -(rect.Width * sprite.Pivot.X);
                    rect.Y = -(rect.Height * sprite.Pivot.Y);

                    _renderer.Begin();

                    _renderer.FillRectangle(rect, sprite.Color, sprite.Texture);
                    
                    _renderer.End();
                }

                var textComponent = default(TextComponent);
                if (_registry.TryGetComponent(renderable, ref textComponent))
                {
                    var font = textComponent.Font ?? _deathFont;
                    var text = textComponent.Text ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    if (textComponent.WrapWidth > 0 && textComponent.WrapMode != TextWrapMode.None)
                    {
                        text = textComponent.WrapMode switch
                        {
                            TextWrapMode.WordWrap => TextBlock.WordWrap(font, text, textComponent.WrapWidth),
                            TextWrapMode.LetterWrap => TextBlock.LetterWrap(font, text, textComponent.WrapWidth),
                            _ => text
                        };
                    }

                    var measure = font.MeasureString(text);

                    var pos = -(measure * textComponent.Pivot);

                    var lines = text.Split(Environment.NewLine);

                    _renderer.Begin();
                    
                    foreach (var line in lines)
                    {
                        var lineMeasure = font.MeasureString(line);
                        
                        var y = pos.Y;
                        var x = textComponent.TextAlign switch
                        {
                            TextAlign.Left => pos.X,
                            TextAlign.Center => pos.X + ((measure.X - lineMeasure.X) / 2),
                            TextAlign.Right => pos.X + (measure.X - lineMeasure.X),
                            _ => pos.X
                        };

                        pos.Y += lineMeasure.Y;

                        _renderer.DrawString(font, line, new Vector2(x, y), textComponent.Color);
                    }

                    _renderer.End();
                }
            }
            
            // BEGIN GUI RENDER PASS
            _sceneGui.Render(gameTime);
            
            // End render pass, let's switch to the normal draw buffer.
            _gameLoop.Graphics.SetRenderTarget(null);
            
            // Now let's let the post-process system kick in. This gets the scene onto the screen.
            _postProcessSystem.Process(_sceneRenderTarget);
        }

        /// <summary>
        /// Gets an instance of the scene's primary camera object.
        /// </summary>
        protected SceneObject PrimaryCamera
        {
            get
            {
                var view = _registry.View<Transform, CameraComponent>();

                if (view.Any())
                    return new SceneObject(_registry, view.Last(), this);
                else
                    return null;
            }
        }
        
        /// <summary>
        /// Called every time the engine ticks.
        /// </summary>
        /// <param name="gameTime">A value representing the time since last tick.</param>
        protected virtual void OnUpdate(GameTime gameTime) {}
        
        /// <summary>
        /// Called when the scene loads.
        /// </summary>
        protected virtual void OnLoad() {}
        
        /// <summary>
        /// Called when the scene un-loads.
        /// </summary>
        protected virtual void OnUnload() {}
        
        /// <summary>
        /// Finds a scene object by name.
        /// </summary>
        /// <param name="name">The name of the object to find.</param>
        /// <returns>The first matching object, or null if none was found.</returns>
        public SceneObject FindObjectByName(string name)
        {
            var nameView = _registry.View<string>();

            if (nameView.Any())
            {
                var entity = nameView.First();
                return new SceneObject(_registry, entity, this);
            }

            return null;
        }
        
        /// <summary>
        /// Spawns a new scene object.
        /// </summary>
        /// <returns>The newly created empty scene object.</returns>
        public SceneObject SpawnObject()
        {
            var entity = _registry.Create();
            var obj = new SceneObject(_registry, entity, this);

            var guid = Guid.NewGuid().ToString();

            obj.AddComponent(guid);
            
            return obj;
        }

        private void EnsureSceneRenderTargetSize()
        {
            if (_sceneRenderTarget == null)
            {
                _sceneRenderTarget =
                    new RenderTarget2D(_gameLoop.Graphics, _gameLoop.Window.Width, _gameLoop.Window.Height);
                
                // Tell the post-processor to resize its buffers.
                _postProcessSystem.ReallocateEffectBuffers(_sceneRenderTarget.Width, _sceneRenderTarget.Height);
            }
            else
            {
                if (_sceneRenderTarget.Width != _gameLoop.Window.Width ||
                    _sceneRenderTarget.Height != _gameLoop.Window.Height)
                {
                    _sceneRenderTarget.Dispose();
                    _sceneRenderTarget =
                        new RenderTarget2D(_gameLoop.Graphics, _gameLoop.Window.Width, _gameLoop.Window.Height);
                    
                    // Tell the post-processor to resize its buffers.
                    _postProcessSystem.ReallocateEffectBuffers(_sceneRenderTarget.Width, _sceneRenderTarget.Height);
                }
            }
        }

        [Cheat]
        internal void NoClip(bool value)
        {
            _noClip = value;
        }

        [Cheat]
        internal void Play()
        {
            _paused = false;
        }

        [Cheat]
        internal void PrimaryCamPos()
        {
            Logger.GetLogger().Log(PrimaryCamera.GetComponent<Transform>().ToString());
        }
        
        [Cheat]
        internal void Pause()
        {
            _paused = true;
        }
        
        [Cheat]
        internal void SetCamMode(CameraProjectionType mode)
        {
            PrimaryCamera.GetComponent<CameraComponent>().ProjectionType = mode;
        }

        private void RegisterSystemInternal(ISystem system)
        {
            _systems.Add(system);
            system.Init(this);
        }
        
        /// <summary>
        /// Creates and registers a new system for use only in this scene.
        /// </summary>
        /// <typeparam name="T">The type of system to create.</typeparam>
        /// <returns>An instance of the newly created system.</returns>
        protected T RegisterSystem<T>() where T : ISystem, new()
        {
            var system = new T();
            RegisterSystemInternal(system);
            return system;
        }

        /// <summary>
        /// Gets an instance of a given type of system.
        /// </summary>
        /// <typeparam name="T">The type of system to find.</typeparam>
        /// <returns>An instance of the found system.</returns>
        public T GetSystem<T>() where T : ISystem
        {
            return _systems.OfType<T>().First();
        }
    }
}
