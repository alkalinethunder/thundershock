using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Audio;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;
using Thundershock.Debugging;
using Thundershock.Input;
using Thundershock.Rendering;

namespace Thundershock
{
    public abstract class Scene
    {
        private bool _noClip;
        private CameraManager _cameraManager;
        private GameLayer _gameLoop;
        private List<SceneComponent> _components = new List<SceneComponent>();
        private Font _debugFont;
        private Renderer2D _renderer;
        private InputSystem _input = new();

        public GameLayer Game => _gameLoop;
        public InputSystem InputSystem => _input;
        
        public Camera Camera
        {
            get => _cameraManager.ActiveCamera;
            set => _cameraManager.ActiveCamera = value;
        }
        public AudioBackend Audio => _gameLoop.Audio;
        public Rectangle ViewportBounds
            => _gameLoop.ViewportBounds;
        
        public GraphicsProcessor Graphics => _gameLoop.Graphics;

        public Scene()
        {
            // Create the camera manager for this scene.
            _cameraManager = new(this);
        }
        
        public bool HasComponent<T>() where T : SceneComponent
        {
            return _components.Any(x => x is T);
        }

        public void AddComponent(SceneComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));
            if (component.Scene != null)
                throw new InvalidOperationException("Scene component already belongs to a Scene.");
            _components.Add(component);
            component.Load(this);
        }

        public void GoToScene<T>() where T : Scene, new ()
        {
            _gameLoop.LoadScene<T>();
        }
        
        public T GetComponent<T>() where T : SceneComponent
        {
            return _components.OfType<T>().First();
        }

        public T AddComponent<T>() where T : SceneComponent, new()
        {
            var component = new T();
            AddComponent(component);
            return component;
        }

        public void RemoveComponent(SceneComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            component.Unload();
            _components.Remove(component);
        }

        internal void Load(GameLayer gameLoop)
        {
            _gameLoop = gameLoop ?? throw new ArgumentNullException(nameof(gameLoop));
            _debugFont = Font.GetDefaultFont(_gameLoop.Graphics);
            _renderer = new Renderer2D(_gameLoop.Graphics);
            OnLoad();
            
            Game.GetComponent<CheatManager>().AddCheat("cam.setPersp", () => Camera.ProjectionType = CameraProjectionType.Perspective);
            Game.GetComponent<CheatManager>().AddCheat("cam.setOrtho",
                () => Camera.ProjectionType = CameraProjectionType.Orthographic);

            Game.GetComponent<CheatManager>().AddCheat("cam.orthoWidth", (args) =>
            {
                if (float.TryParse(args.First(), out var width))
                {
                    Camera.OrthoRight = width;
                }
            });
            
            Game.GetComponent<CheatManager>().AddCheat("cam.orthoHeight", (args) =>
            {
                if (float.TryParse(args.First(), out var height))
                {
                    Camera.OrthoBottom = height;
                }
            });
            
            Game.GetComponent<CheatManager>().AddCheat("noclip",
                () => _noClip = !_noClip);
        }

        internal void MouseDown(MouseButtonEventArgs e)
        {
            _input.FireMouseDown(e);
        }

        internal void MouseUp(MouseButtonEventArgs e)
        {
            _input.FireMouseUp(e);
        }

        internal void MouseMove(MouseMoveEventArgs e)
        {
            _input.FireMouseMove(e);
        }

        internal void MouseScroll(MouseScrollEventArgs e)
        {
            _input.FireMouseScroll(e);
        }
        
        internal bool KeyChar(KeyCharEventArgs e)
        {
            _input.FireKeyChar(e);
            return true;
        }
        
        internal bool KeyUp(KeyEventArgs e)
        {
            _input.FireKeyUp(e);
            return true;
        }
        
        internal bool KeyDown(KeyEventArgs e)
        {
            if (_noClip)
            {
                switch (e.Key)
                {
                    case Keys.W:
                        Camera.Transform.Position = new Vector3(Camera.Transform.Position.X,
                            Camera.Transform.Position.Y, Camera.Transform.Position.Z + 0.25f);
                        break;
                    case Keys.S:
                        Camera.Transform.Position = new Vector3(Camera.Transform.Position.X,
                            Camera.Transform.Position.Y, Camera.Transform.Position.Z - 0.25f);
                        break;
                    
                    case Keys.Left:
                        Camera.Transform.Position = new Vector3(Camera.Transform.Position.X - 0.25f,
                            Camera.Transform.Position.Y, Camera.Transform.Position.Z);
                        break;
                    case Keys.Right:
                        Camera.Transform.Position = new Vector3(Camera.Transform.Position.X + 0.25f,
                            Camera.Transform.Position.Y, Camera.Transform.Position.Z);
                        break;
                    case Keys.Down:
                        Camera.Transform.Position = new Vector3(Camera.Transform.Position.X,
                            Camera.Transform.Position.Y - 0.25f, Camera.Transform.Position.Z);
                        break;
                    case Keys.Up:
                        Camera.Transform.Position = new Vector3(Camera.Transform.Position.X,
                            Camera.Transform.Position.Y + 0.25f, Camera.Transform.Position.Z);
                        break;
                }

                return true;
            }
            else
            {
                _input.FireKeyDown(e);
                return true;
            }
        }
        
        public void Unload()
        {
            while (_components.Any())
                RemoveComponent(_components.First());

            OnUnload();
            _gameLoop = null;
        }

        internal void Update(GameTime gameTime)
        {
            OnUpdate(gameTime);

            foreach (var component in _components.ToArray())
            {
                component.Update(gameTime);
            }
        }

        public void Draw(GameTime gameTime)
        {
            var projection = Camera.ProjectionMatrix;

            _renderer.ProjectionMatrix = projection;
            
            foreach (var component in _components)
                component.Draw(gameTime, _renderer);
        }


        protected virtual void OnUpdate(GameTime gameTime) {}
        protected virtual void OnLoad() {}
        protected virtual void OnUnload() {}

        #region  CordinateHelpers

        public Vector2 ViewportToScreen(Vector2 coordinates)
        {
            return coordinates;
        }

        public Vector2 ScreenToViewport(Vector2 coordinates)
        {
            return coordinates;
        }


        #endregion
    }
}
