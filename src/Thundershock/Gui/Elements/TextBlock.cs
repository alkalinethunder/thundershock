using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Thundershock.Core;
using Thundershock.Gui.Styling;
using Rectangle = Thundershock.Core.Rectangle;


namespace Thundershock.Gui.Elements
{
    public class TextBlock : ContentElement
    {
        private Font _lastFont;
        private bool _textIsDirty = true;
        private List<Line> _lines = new();
        private string _text = "Text Block";
        private TextAlign _textAlign = TextAlign.Left;
        private TextWrapMode _wrapMode = TextWrapMode.WordWrap;
        private float _lastWidth;
        
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    _textIsDirty = true;
                }
            }
        }

        public TextAlign TextAlign
        {
            get => _textAlign;
            set
            {
                if (_textAlign != value)
                {
                    _textAlign = value;
                    _textIsDirty = true;
                }
            }
        }

        public TextWrapMode WrapMode
        {
            get => _wrapMode;
            set
            {
                if (_wrapMode != value)
                {
                    _wrapMode = value;
                    _textIsDirty = true;
                }
            }
        }
        
        private Font GetFont()
        {
            return Font.GetFont(GuiSystem.Style.GetFont(this));
        }

        public static string LetterWrap(Font font, string text, float wrapWidth)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (wrapWidth <= 0)
                return text;

            var lineWidth = 0f;
            var sb = new StringBuilder();

            foreach (var ch in text)
            {
                var m = font.MeasureString(ch.ToString()).X;
                if (lineWidth + m > wrapWidth)
                {
                    sb.AppendLine();
                    lineWidth = 0;
                }

                lineWidth += m;
                sb.Append(ch);
            }
            
            return sb.ToString();
        }

        public static string WordWrap(Font font, string text, float wrapWidth)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
        
            if (wrapWidth <= 0)
                return text;
            
            // Resulting wrapped text.
            var sb = new StringBuilder();
            
            // Break lines.
            var lines = text.Split(Environment.NewLine);
            var isFirstLine = true;
            // go through each line.
            foreach (var line in lines)
            {
                if (!isFirstLine)
                    sb.AppendLine();
                
                // first step: break words.
                var words = new List<string>();
                var w = "";
                for (var i = 0; i <= line.Length; i++)
                {
                    if (i < line.Length)
                    {
                        var ch = line[i];
                        w += ch;
                        if (char.IsWhiteSpace(ch))
                        {
                            words.Add(w);
                            w = "";
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(w))
                        {
                            words.Add(w);
                        }
                    }
                }

                // step 2: Line-wrap.
                var lineWidth = 0f;
                for (var i = 0; i < words.Count; i++)
                {
                    var word = words[i];
                    var m = font.MeasureString(word).X;
                    var m2 = font.MeasureString(word.Trim()).X;
                
                    if (lineWidth + m > wrapWidth && lineWidth > 0) // this makes the whole thing a lot less greedy
                    {
                        sb.AppendLine();
                        lineWidth = 0;
                    }

                    if (m2 > lineWidth)
                    {
                        var letterWrapped = LetterWrap(font, word, wrapWidth);
                        var letterLines = letterWrapped.Split(Environment.NewLine);
                        var last = letterLines.Last();

                        m = font.MeasureString(last).X;

                        sb.Append(letterWrapped);
                    }
                    else
                    {
                        sb.Append(word);
                    }

                    lineWidth += m;
                }

                isFirstLine = false;
            }
            
            return sb.ToString();
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            var pos = contentRectangle.Location;
            var height = (_lastFont ?? GetFont()).LineHeight;
            
            foreach (var line in _lines)
            {
                pos.X = _textAlign switch
                {
                    TextAlign.Right => contentRectangle.Right - line.Measure.X,
                    TextAlign.Center => contentRectangle.Left + ((contentRectangle.Width - line.Measure.X) / 2),
                    _ => contentRectangle.Left
                };

                line.Position = pos;
                
                pos.Y += height;
            }

            base.ArrangeOverride(contentRectangle);
        }

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            if (Math.Abs(_lastWidth - alottedSize.X) >= 0.001f)
            {
                _textIsDirty = true;
            }

            if (_textIsDirty)
            {
                _lastFont = GetFont();
                
                var wrapped = _wrapMode switch
                {
                    TextWrapMode.WordWrap => WordWrap(_lastFont, _text, alottedSize.X),
                    TextWrapMode.LetterWrap => LetterWrap(_lastFont, _text, alottedSize.X),
                    _ => _text
                } ?? string.Empty;

                _lines.Clear();

                foreach (var line in wrapped.Split(Environment.NewLine))
                {
                    var ln = new Line
                    {
                        Text = line,
                        Measure = _lastFont.MeasureString(line)
                    };

                    _lines.Add(ln);
                }
                
                _textIsDirty = false;
            }
            
            var size = Vector2.Zero;
            foreach (var line in _lines)
            {
                var m = line.Measure;

                size.X = Math.Max(size.X, m.X);
                size.Y += _lastFont.LineHeight;
            }

            _lastWidth = size.X;
            
            return size;
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            if (_lines.Count > 0)
            {
                var f = _lastFont;
                var color = (ForeColor ?? StyleColor.Default).GetColor(GuiSystem.Style.DefaultForeground);
                foreach (var line in _lines)
                {
                    renderer.DrawString(f, line.Text, line.Position, color);
                }
            }
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            var f = GetFont();
            if (f != _lastFont)
            {
                _lastFont = f;
                _textIsDirty = true;
            }
            
            base.OnUpdate(gameTime);
        }

        private class Line
        {
            public string Text;
            public Vector2 Position;
            public Vector2 Measure;
        }
    }
}