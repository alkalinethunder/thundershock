using Thundershock.Core;
using Thundershock.OpenGL;

namespace Thundershock.Editor
{
    public class EditorApp : GraphicalAppBase
    {
        private GameLayer _game;
        private EditorLayer _editor;
        
        protected override GameWindow CreateGameWindow()
        {
            return new SdlGameWindow();
        }

        protected override void OnPreInit()
        {
            Window.CanResize = true;
            Window.IsBorderless = false;
            Window.IsFullScreen = false;

            base.OnPreInit();
        }

        protected override void OnPostInit()
        {
            var scene = LoadScene<EditorScene>();
            
            _game = LayerManager.GetFirstLayer<GameLayer>();
            _editor = new EditorLayer(_game, scene);

            LayerManager.PushOverlay(_editor);

            base.OnPostInit();
        }
    }
}