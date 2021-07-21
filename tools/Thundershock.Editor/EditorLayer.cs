using Thundershock.Core;
using Thundershock.Core.Input;
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

        #region Menu Items

        private MenuItem _fileMenu = new("File");
        private MenuItem _editMenu = new("Edit");
        private MenuItem _viewMenu = new("View");
        private MenuItem _windowMenu = new("Window");
        private MenuItem _helpMenu = new("Help");

        #endregion

        #region Menu: File

        private MenuItem _newScene = new("New Scene...");
        private MenuItem _openScene = new("Open Scene");
        private MenuItem _saveScene = new("save Scene");
        private MenuItem _saveSceneAs = new("Save Scene As...");
        private MenuItem _newProject = new("New Project...");
        private MenuItem _openProject = new("Open Project...");
        private MenuItem _saveAllAssets = new("Save All Goodies...");
        private MenuItem _importAsset = new("Import Goodie...");
        private MenuItem _exportAssets = new("Export Goodie...");
        private MenuItem _publish = new("Publish Game...");
        private MenuItem _exit = new("Exit");

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

            _gui.LoadStyle<EditorStyle>();

            BuildGui();
            BuildMenu();
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

        private void BuildMenu()
        {
            _menuBar.Items.Add(_fileMenu);
            _menuBar.Items.Add(_editMenu);
            _menuBar.Items.Add(_viewMenu);
            _menuBar.Items.Add(_windowMenu);
            _menuBar.Items.Add(_helpMenu);
 
            _fileMenu.Items.Add(_newProject);
            _fileMenu.Items.Add(_openProject);
            _fileMenu.Items.Add(_saveAllAssets);
            _fileMenu.Items.Add(_newScene);
            _fileMenu.Items.Add(_openScene);
            _fileMenu.Items.Add(_saveScene);
            _fileMenu.Items.Add(_saveSceneAs);
            _fileMenu.Items.Add(_importAsset);
            _fileMenu.Items.Add(_exportAssets);
            _fileMenu.Items.Add(_publish);
            _fileMenu.Items.Add(_exit);
        }

        #endregion

        #region Layer Events

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            return _gui.MouseDown(e);
        }

        public override bool MouseMove(MouseMoveEventArgs e)
        {
            return _gui.MouseMove(e);
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return _gui.MouseUp(e);
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            return _gui.KeyDown(e);
        }

        public override bool KeyUp(KeyEventArgs e)
        {
            return _gui.KeyUp(e);
        }

        public override bool KeyChar(KeyCharEventArgs e)
        {
            return _gui.KeyChar(e);
        }

        #endregion
    }
}