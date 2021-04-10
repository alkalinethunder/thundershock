﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Thundershock.Input;

namespace Thundershock.Gui.Elements
{
    public class TextEntry : Element
    {
        private int _inputPos = 0;
        private string _text = string.Empty;
        private string _hint = "Enter text...";

        public Color TextColor { get; set; } = Color.Black;
        public Color HintColor { get; set; } = Color.Gray;
        
        public SpriteFont Font { get; set; }

        public event EventHandler TextCommitted;
        
        private SpriteFont GetFont()
        {
            return Font ?? GuiSystem.Style.DefaultFont;
        }

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            var f = GetFont();

            var text = _text ?? string.Empty;
            var hint = _hint ?? string.Empty;

            var mText = text.Length > hint.Length ? text : hint;

            var m = f.MeasureString(mText);
            m.X += GuiSystem.Style.TextCursorWidth;

            m.Y = Math.Max(m.Y, f.LineSpacing);

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
                    }
                    break;
                case Keys.Back:
                    if (_inputPos > 0)
                    {
                        _inputPos--;
                        _text = _text.Remove(_inputPos, 1);
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

            if (f.Characters.Contains(e.Character))
            {
                _text = _text.Insert(_inputPos, e.Character.ToString());

                _inputPos++;

                return true;
            }

            return base.OnKeyChar(e);
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            var font = GetFont();

            var pos = new Vector2(ContentRectangle.Left,
                ContentRectangle.Top + ((ContentRectangle.Height - font.LineSpacing) / 2));
            
            if (string.IsNullOrEmpty(_text))
            {
                renderer.DrawString(font, _hint, pos, HintColor);
            }

            renderer.DrawString(font, _text, pos, TextColor);

            if (HasAnyFocus)
            {
                var cursorPos = pos;
                var m = _text.Substring(0, _inputPos);
                var measure = font.MeasureString(m);
                cursorPos.X += measure.X;

                GuiSystem.Style.DrawTextCursor(renderer, TextColor, cursorPos, font.LineSpacing);
            }
        }
    }
}