using System;
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
        
        public AppBase App => _app;

        public string Title
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                if (_app != null)
                    OnWindowTitleChanged();
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
        
        protected virtual void OnWindowTitleChanged() {}
    }
}