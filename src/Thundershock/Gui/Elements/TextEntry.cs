using System;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Input;

namespace Thundershock.Gui.Elements
{
    public class TextEntry : Element
    {
        private int _inputPos = 0;
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
            set => _hint = value ?? string.Empty;
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
                }
            }
        }
        
        public event EventHandler TextCommitted;
        public event EventHandler TextChanged;
        
        private Font GetFont()
        {
            return Font.GetFont(GuiSystem.Style.DefaultFont);
        }

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            var f = GetFont();

            var text = _text ?? string.Empty;
            var hint = _hint ?? string.Empty;

            var mText = text.Length > hint.Length ? text : hint;

            var m = f.MeasureString(mText);
            m.X += GuiSystem.Style.TextCursorWidth;

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
                    }
                    break;
                case Keys.Backspace:
                    if (_inputPos > 0)
                    {
                        _inputPos--;
                        _text = _text.Remove(_inputPos, 1);
                        TextChanged?.Invoke(this, EventArgs.Empty);
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
            var f = GetFont();

            _text = _text.Insert(_inputPos, e.Character.ToString());
            TextChanged?.Invoke(this, EventArgs.Empty);
            _inputPos++;

            return true;
        }
        

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            var font = GetFont();

            var m = _text;
            if (string.IsNullOrEmpty(m))
                m = _hint ?? string.Empty;
            var measure = font.MeasureString(m);

            var pos = new Vector2(ContentRectangle.Left,
                ContentRectangle.Top + ((ContentRectangle.Height - measure.Y) / 2));

            var textColor = ForeColor.GetColor(GuiSystem.Style.DefaultForeground);
            
            if (string.IsNullOrEmpty(_text))
            {
                renderer.DrawString(font, _hint, pos, textColor * 0.5f);
            }

            renderer.DrawString(font, _text, pos, textColor);

            if (HasAnyFocus)
            {
                var cursorPos = pos;
                m = _text.Substring(0, _inputPos);
                measure = font.MeasureString(m);
                cursorPos.X += measure.X;

                GuiSystem.Style.DrawTextCursor(renderer, textColor, cursorPos, font.LineSpacing);
            }
        }
    }
}