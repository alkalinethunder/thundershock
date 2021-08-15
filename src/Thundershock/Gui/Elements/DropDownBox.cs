using System;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Input;


namespace Thundershock.Gui.Elements
{
    public class DropDownBox : LayoutElement
    {
        private int _lastIndex;
        private AdvancedButton _activator = new();
        private TextBlock _text = new();
        private Stacker _activatorContent = new();
        private Picture _arrow = new();
        private StringList _itemList = new();
        private ScrollPanel _scrollPanel = new();

        private bool _isOpen;

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

            _text.Text = "Select Item...";
        }

        private void ItemListOnBlurred(object sender, FocusChangedEventArgs e)
        {
            if (_isOpen)
            {
                _scrollPanel.RemoveFromParent();
                _isOpen = false;
            }
        }

        private void ItemListOnSelectedIndexChanged(object sender, EventArgs e)
        {
            this._text.Text = _itemList.SelectedItem;

            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);

            _scrollPanel.RemoveFromParent();

            _isOpen = false;
        }

        private void ActivatorOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isOpen)
            {
                _scrollPanel.RemoveFromParent();
                _isOpen = false;
            }
            else
            {
                GuiSystem.AddToViewport(_scrollPanel);

                _scrollPanel.IsInteractable = true;
                _scrollPanel.ViewportAnchor = FreePanel.CanvasAnchor.TopLeft;
                _scrollPanel.ViewportPosition =
                    new Vector2(this.BoundingBox.Left, this.BoundingBox.Bottom);
                
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
    }
}