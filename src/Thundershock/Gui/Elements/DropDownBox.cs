using System;
using Thundershock.Core;
using Thundershock.Core.Input;


namespace Thundershock.Gui.Elements
{
    public class DropDownBox : Element
    {
        private int _lastIndex;
        private AdvancedButton _activator = new();
        private TextBlock _text = new();
        private Stacker _activatorContent = new();
        private Picture _arrow = new();
        private StringList _itemList = new();
        private ScrollPanel _scrollPanel = new();

        private bool _isOpen = false;

        public string SelectedItem => _itemList.SelectedItem;

        public event EventHandler SelectedIndexChanged;
        
        public int SelectedIndex
        {
            get => _itemList.SelectedIndex;
            set => _itemList.SelectedIndex = value;
        }

        public void AddItem(string value)
        {
            _itemList.AddItem(value);
        }

        public void RemoveItem(string value)
        {
            _itemList.RemoveItem(value);
        }

        public void Clear()
        {
            _itemList.Clear();
        }

        public DropDownBox()
        {
            _activator.CanFocus = false;
            _scrollPanel.Children.Add(_itemList);
            _scrollPanel.IsInteractable = true;
            
            _activatorContent.Children.Add(_text);
            _activatorContent.Children.Add(_arrow);
            
            _activator.Children.Add(_activatorContent);
            
            Children.Add(_activator);
            
            _activator.MouseUp += ActivatorOnMouseUp;
            _itemList.SelectedIndexChanged += ItemListOnSelectedIndexChanged;
            _itemList.Blurred += ItemListOnBlurred;
        }

        private void ItemListOnBlurred(object? sender, FocusChangedEventArgs e)
        {
            if (_isOpen)
            {
                _scrollPanel.Parent.Children.Remove(_scrollPanel);
                _isOpen = false;
            }
        }

        private void ItemListOnSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_itemList.SelectedIndex != _lastIndex && _isOpen)
            {
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);

                _scrollPanel.Parent.Children.Remove(_scrollPanel);

                _isOpen = false;
            }
        }

        private void ActivatorOnMouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (_isOpen)
            {
                _scrollPanel.Parent.Children.Remove(_scrollPanel);
                _isOpen = false;
            }
            else
            {
                GuiSystem.AddToViewport(_scrollPanel);

                _scrollPanel.Properties.SetValue(FreePanel.AutoSizeProperty, true);

                var anchor = FreePanel.CanvasAnchor.TopLeft;
                anchor.Left = BoundingBox.Left / (float) GuiSystem.BoundingBox.Width;
                anchor.Top = BoundingBox.Bottom / (float) GuiSystem.BoundingBox.Height;
                
                _scrollPanel.Properties.SetValue(FreePanel.AnchorProperty, anchor);
                
                _scrollPanel.MaximumHeight = 300;
                
                _lastIndex = SelectedIndex;
                _isOpen = true;
                
                GuiSystem.SetFocus(_itemList);
            }
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            _itemList.MinimumWidth = contentRectangle.Width;
            
            base.ArrangeOverride(contentRectangle);
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            _text.ForeColor = GuiSystem.Style.GetButtonTextColor(_activator);
            
            if (_itemList.SelectedIndex > -1)
            {
                _text.Text = _itemList.SelectedItem;
            }
            else
            {
                _text.Text = "Select item...";
            }
            
            base.OnUpdate(gameTime);
        }
    }
}