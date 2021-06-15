using System;

namespace Thundershock.Core
{
    public abstract class GameWindow
    {
        private AppBase _app;

        public AppBase App => _app;
        
        public string Title { get; set; }
        public bool IsBorderless { get; set; }
        public bool IsFullScreen { get; set; }
        public bool IsUserResizeable { get; set; }

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
    }
}