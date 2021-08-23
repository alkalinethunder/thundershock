using System;
using Thundershock.Core;
using System.Numerics;
using Thundershock.Gui.Styling;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;

namespace Thundershock.Gui.Elements
{
    public class Button : ContentElement, IButtonElement
    {
        private Font _lastFont;
        private TextRenderBuffer _textCache;
        private string _text = "Button Text";
        private bool _isPressed;
        private bool _isHovered;
        private string _wrapped;
        
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? string.Empty;
                    InvalidateLayout();
                }
            }
        }

        public Button()
        {
            CanFocus = true;
            IsInteractable = true;
            Margin = new Padding(7, 4);
        }

        public bool IsPressed => _isPressed;
        public bool IsHovered => _isHovered;
        public bool IsActive { get; set; } = false;
        public StyleColor ButtonColor { get; set; } = StyleColor.Default;
        public StyleColor ButtonActiveColor { get; set; } = StyleColor.Default;
        
        protected override bool OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Primary)
            {
                _isPressed = true;
            }
            
            return base.OnMouseDown(e);
        }

        protected override bool OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Primary)
            {
                _isPressed = false;
            }
            
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
            _isPressed = false;
            return base.OnMouseLeave(e);
        }

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            _textCache = null;
            var font = Font.GetFont(GuiSystem.Style.GetFont(this));
            return font.MeasureString(Text);
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            var font = Font.GetFont(GuiSystem.Style.GetFont(this));

            var text = Text;
            _wrapped = TextBlock.WordWrap(font, text, contentRectangle.Width);
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            var font = Font.GetFont(GuiSystem.Style.GetFont(this));

            if (font != _lastFont)
            {
                _lastFont = font;
                _textCache = null;
            }
            
            var textColor = GuiSystem.Style.GetButtonTextColor(this);
            renderer.ComputeColor(ref textColor);

            if (_textCache != null && (_textCache.Color != textColor || _textCache.Depth != renderer.Layer))
            {
                _textCache = null;
            }

            if (_textCache == null)
            {
                var lines = _wrapped.Split(Environment.NewLine);
                var pos = ContentRectangle.Location;

                pos.Y = ContentRectangle.Top + ((ContentRectangle.Height - (font.LineHeight * lines.Length)) / 2);

                foreach (var line in lines)
                {
                    var m = font.MeasureString(line);
                    pos.X = ContentRectangle.Left + ((ContentRectangle.Width - m.X) / 2);

                    if (_textCache == null)
                    {
                        _textCache = font.Draw(line, pos, textColor, renderer.Layer);
                    }
                    else
                    {
                        font.Draw(_textCache, line, pos, textColor, renderer.Layer);
                    }
                    
                    pos.Y += font.LineHeight;
                }


            }
            
            
            GuiSystem.Style.DrawButton(renderer, this);
            renderer.DrawText(_textCache);
        }
    }
}