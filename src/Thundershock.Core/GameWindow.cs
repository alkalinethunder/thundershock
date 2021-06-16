using System;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;

namespace Thundershock.Core
{
    public abstract class GameWindow
    {
        private AppBase _app;
        private string _windowTitle = "Thundershock Engine";
        private bool _borderless;
        private bool _fullscreen;
        private bool _resizeable;
        private int _width = 640;
        private int _height = 480;
        
        public AppBase App => _app;

        public string Title
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    if (_app != null)
                        OnWindowTitleChanged();
                }
            }
        }

        public bool IsBorderless
        {
            get => _borderless;
            set
            {
                if (_borderless != value)
                {
                    _borderless = value;
                    if (_app != null)
                        OnWindowModeChanged();
                }
            }
        }
        
        public bool IsFullScreen
        {
            get => _fullscreen;
            set
            {
                if (_fullscreen != value)
                {
                    _fullscreen = value;
                    if (_app != null)
                        OnWindowModeChanged();
                }
            }
        }

        public int Width
        {
            get => _width;
            set
            {
                if (_width != value)
                {
                    if (value <= 0)
                        throw new InvalidOperationException("Window size must be greater than zero.");

                    _width = value;

                    if (_app != null)
                    {
                        OnClientSizeChanged();
                    }
                }
            }
        }
        
        public int Height
        {
            get => _height;
            set
            {
                if (_height != value)
                {
                    if (value <= 0)
                        throw new InvalidOperationException("Window size must be greater than zero.");

                    _height = value;

                    if (_app != null)
                    {
                        OnClientSizeChanged();
                    }
                }
            }
        }

        
        public abstract Renderer Renderer { get; }
        
        public void Show(AppBase app)
        {
            if (_app != null)
                throw new InvalidOperationException("Window is already open.");

            _app = app ?? throw new ArgumentNullException(nameof(app));

            Initialize();
        }

        public void Update()
        {
            if (_app == null)
                throw new InvalidOperationException("Window is not open.");

            OnUpdate();
        }

        public void Close()
        {
            if (_app == null)
                throw new InvalidOperationException("Window already closed...");

            OnClosed();
        }

        protected abstract void OnClosed();
        protected abstract void OnUpdate();
        protected abstract void Initialize();
        
        protected virtual void OnClientSizeChanged() {}
        protected virtual void OnWindowTitleChanged() {}
        protected virtual void OnWindowModeChanged() {}

        protected void DispatchKeyEvent(Keys key, char character, bool isPressed, bool isRepeated, bool isText)
        {
            var evt = null as KeyEventArgs;

            if (isText)
            {
                evt = new KeyCharEventArgs(key, character);
                KeyChar?.Invoke(this, evt as KeyCharEventArgs);
            }
            else
            {
                evt = new KeyEventArgs(key);

                if (isPressed)
                {
                    KeyDown?.Invoke(this, evt);
                }
                else
                {
                    KeyUp?.Invoke(this, evt);
                }
            }
        }
        
        public event EventHandler<KeyEventArgs> KeyDown;
        public event EventHandler<KeyEventArgs> KeyUp;
        public event EventHandler<KeyCharEventArgs> KeyChar;

    }
}