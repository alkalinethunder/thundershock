using Thundershock.Core;
using Thundershock.Core.Rendering;
using Thundershock.Gui;
using Thundershock.Gui.Elements;

namespace Thundershock.Editor
{
    public sealed class EditorLayer : Layer
    {
        #region Layer references

        private GameLayer _game;

        #endregion

        #region Scene

        private EditorScene _scene;

        #endregion

        #region Components

        private GuiSystem _gui;
        private Renderer2D _editorRenderer;
        
        #endregion

        #region GUI elements

        private Stacker _master = new();
        private Panel _headerArea = new();
        private Stacker _headerStack = new();
        private MenuBar _menuBar = new();
        private Stacker _toolbar = new();
        private Panel _contentPanel = new();
        private Panel _viewportPanel = new();
        private Panel _tweakerPanel = new();
        private Panel _scenePanel = new();
        private Panel _goodiesBag = new();

        // TODO: Docking
        private Stacker _editorStack = new();
        private Stacker _middleStack = new();
        private Stacker _rightStack = new();
        
        #endregion
        
        #region Layer Implementation
        
        public EditorLayer(GameLayer game, EditorScene scene)
        {
            _game = game;
            _scene = scene;
        }
        
        protected override void OnInit()
        {
            _editorRenderer = new Renderer2D(App.Graphics);
            _gui = new GuiSystem(App.Graphics);

            BuildGui();
        }

        protected override void OnUnload()
        {
            
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            // Set the virtual viewport of the UI to match the window size.
            _gui.SetViewportSize(App.Window.Width, App.Window.Height);

            _gui.Update(gameTime);
            
            // Set up the scene viewport.
            _game.OverrideViewport = true;
            _game.ViewportOverrideBounds = _viewportPanel.BoundingBox;
        }

        protected override void OnRender(GameTime gameTime)
        {
            _gui.Render(gameTime);
        }

        #endregion

        #region GUI Layout Stuff

        private void BuildGui()
        {
            _editorStack.Direction = StackDirection.Horizontal;
            
            // TODO: Resizable panels.
            _contentPanel.FixedHeight = 315;
            _goodiesBag.FixedWidth = 380;
            _rightStack.FixedWidth = 380;
            
            // Viewport background needs to be transparent so that we can see the scene.
            _viewportPanel.BackColor = Color.Transparent;
            
            _scenePanel.Properties.SetValue(Stacker.FillProperty, StackFill.Fill);
            _tweakerPanel.Properties.SetValue(Stacker.FillProperty, StackFill.Fill);
            _viewportPanel.Properties.SetValue(Stacker.FillProperty, StackFill.Fill);
            _middleStack.Properties.SetValue(Stacker.FillProperty, StackFill.Fill);
            _editorStack.Properties.SetValue(Stacker.FillProperty, StackFill.Fill);
            
            _headerStack.Children.Add(_menuBar);
            _headerStack.Children.Add(_toolbar);
            
            _headerArea.Children.Add(_headerStack);

            _middleStack.Children.Add(_viewportPanel);
            _middleStack.Children.Add(_contentPanel);

            _rightStack.Children.Add(_scenePanel);
            _rightStack.Children.Add(_tweakerPanel);
            
            _editorStack.Children.Add(_goodiesBag);
            _editorStack.Children.Add(_middleStack);
            _editorStack.Children.Add(_rightStack);
            
            _master.Children.Add(_headerArea);
            _master.Children.Add(_editorStack);

            _gui.AddToViewport(_master);
        }

        #endregion
    }
}