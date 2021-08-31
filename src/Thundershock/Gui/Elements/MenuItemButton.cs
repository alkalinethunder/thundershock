using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;
using Thundershock.Gui.Styling;

namespace Thundershock.Gui.Elements
{
    internal sealed class MenuItemButton : ContentElement
    {
        private TextRenderBuffer _textCache;
        private Font _lastFont;
        private bool _isHovered = false;
        private bool _isPressed = false;
        private float _hPad = 5;
        private float _vPad = 2;
        private string _text = "Menu Item";

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    _textCache = null;
                }
            }
        }

        private Font GetFont()
        {
            return GuiSystem.Style.GetFont(this.Parent.Parent);
        }
        
        public MenuItemButton()
        {
            IsInteractable = true;
            CanFocus = true;
        }

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            var font = GetFont();
            var text = Text;
            var measure = font.MeasureString(text);

            measure.X += _hPad * 2;
            measure.Y += _vPad * 2;

            return measure;
        }

        protected override bool OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Primary)
                _isPressed = true;
            return base.OnMouseDown(e);
        }

        protected override bool OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Primary)
                _isPressed = false;
            return base.OnMouseUp(e);
        }

        protected override bool OnMouseEnter(MouseMoveEventArgs e)
        {
            _isHovered = true;
            return base.OnMouseEnter(e);
        }

        protected override bool OnMouseLeave(MouseMoveEventArgs e)
        {
            _isHovered = false;
            return base.OnMouseLeave(e);
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            var font = GetFont();
            var selState = SelectionStyle.None;
            
            if (_isPressed)
                selState = SelectionStyle.ItemActive;
            else if (_isHovered)
                selState = SelectionStyle.ItemHover;

            GuiSystem.Style.PaintMenuBarItemBackground(this, gameTime, renderer, selState);
            
            if (font != _lastFont)
            {
                _lastFont = font;
                _textCache = null;
            }

            if (_textCache == null || _textCache.Depth != renderer.Layer)
            {
                var text = Text;
                var textPos = ContentRectangle.Location;

                textPos.X += _hPad;
                textPos.Y += _vPad;

                _textCache = GuiSystem.Style.PaintMenuItemText(this, gameTime, renderer, text, font, textPos, selState);
            }

            renderer.DrawText(_textCache);
        }
    }
}