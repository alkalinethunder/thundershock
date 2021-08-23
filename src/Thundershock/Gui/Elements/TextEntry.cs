using System;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;

namespace Thundershock.Gui.Elements
{
    public class TextEntry : ContentElement
    {
        private Font _font;
        private TextRenderBuffer _textCache;
        private int _inputPos;
        private string _text = string.Empty;
        private string _hint = "Enter text...";

        public TextEntry()
        {
            CanFocus = true;
            IsInteractable = true;
        }
        
        public string HintText
        {
            get => _hint;
            set
            {
                if (_hint != value)
                {
                    _hint = value ?? string.Empty;
                    _textCache = null;
                    InvalidateLayout();
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                value ??= string.Empty;

                if (_text != value)
                {
                    if (_inputPos > value.Length)
                        _inputPos = value.Length;
                    _text = value;
                    _textCache = null;
                    InvalidateLayout();
                }
            }
        }
        
        public event EventHandler TextCommitted;
        public event EventHandler TextChanged;
        
        private Font GetFont()
        {
            return Font.GetFont(GuiSystem.Style.GetFont(this));
        }

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            _textCache = null;
            var f = GetFont();

            var text = _text ?? string.Empty;
            var hint = _hint ?? string.Empty;

            var mText = text.Length > hint.Length ? text : hint;

            var m = f.MeasureString(mText);
            m.X += GuiSystem.Style.TextCursorWidth;

            m.Y = f.LineHeight;
            
            return m;
        }

        protected override bool OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Keys.Left:
                    if (_inputPos > 0)
                        _inputPos--;
                    break;
                case Keys.Right:
                    if (_inputPos < _text.Length)
                        _inputPos++;
                    break;
                case Keys.Home:
                    _inputPos = 0;
                    break;
                case Keys.End:
                    _inputPos = _text.Length;
                    break;
                case Keys.Delete:
                    if (_inputPos < _text.Length)
                    {
                        _text = _text.Remove(_inputPos, 1);
                        TextChanged?.Invoke(this, EventArgs.Empty);
                        _textCache = null;
                        InvalidateLayout();
                    }
                    break;
                case Keys.Backspace:
                    if (_inputPos > 0)
                    {
                        _inputPos--;
                        _text = _text.Remove(_inputPos, 1);
                        TextChanged?.Invoke(this, EventArgs.Empty);
                        _textCache = null;
                        InvalidateLayout();
                    }
                    break;
                case Keys.Enter:
                    TextCommitted?.Invoke(this, EventArgs.Empty);
                    break;
            }

            return base.OnKeyDown(e);
        }

        protected override bool OnKeyChar(KeyCharEventArgs e)
        {
            _text = _text.Insert(_inputPos, e.Character.ToString());
            TextChanged?.Invoke(this, EventArgs.Empty);
            _inputPos++;
            _textCache = null;
            InvalidateLayout();

            return true;
        }
        

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            var font = GetFont();

            if (font != _font)
            {
                _font = font;
                _textCache = null;
            }

            if (_textCache != null && _textCache.Depth != renderer.Layer)
                _textCache = null;
            
            var color = ForeColor.GetColor(GuiSystem.Style.DefaultForeground);
            
            var pos = new Vector2(ContentRectangle.Left,
                ContentRectangle.Top + ((ContentRectangle.Height - font.LineHeight) / 2));

            if (_textCache == null)
            {
                var textColor = color;

                renderer.ComputeColor(ref textColor);

                if (string.IsNullOrEmpty(_text))
                {
                    _textCache = font.Draw(_hint, pos, textColor * 0.5f, renderer.Layer);
                }
                else
                {
                    _textCache = font.Draw(_text, pos, textColor, renderer.Layer);
                } 
            }

            renderer.DrawText(_textCache);
            
            if (HasAnyFocus)
            {
                var cursorPos = pos;
                var m = _text.Substring(0, _inputPos);
                var measure = font.MeasureString(m);
                cursorPos.X += measure.X;

                GuiSystem.Style.DrawTextCursor(renderer, color, cursorPos, font.LineHeight);
            }

        }
    }
}