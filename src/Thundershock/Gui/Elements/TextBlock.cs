using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Thundershock.Core;
using Thundershock.Gui.Styling;


namespace Thundershock.Gui.Elements
{
    public class TextBlock : Element
    {
        private string _wrappedText;

        public StyleColor Color { get; set; } = StyleColor.Default;
        public string Text { get; set; } = "Text Block";

        public TextAlign TextAlign { get; set; }

        public TextWrapMode WrapMode { get; set; } = TextWrapMode.WordWrap;
        
        private Font GetFont()
        {
            return Font.GetFont(GuiSystem.Style.DefaultFont);
        }

        public static string LetterWrap(Font font, string text, float wrapWidth)
        {
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
                        word = last;

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
            var f = GetFont();

            switch (WrapMode)
            {
                case TextWrapMode.None:
                    _wrappedText = Text;
                    break;
                case TextWrapMode.LetterWrap:
                    _wrappedText = LetterWrap(f, Text, contentRectangle.Width);
                    break;
                case TextWrapMode.WordWrap:
                    _wrappedText = WordWrap(f, Text, contentRectangle.Width);
                    break;
            }

            
            base.ArrangeOverride(contentRectangle);
        }

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            if (string.IsNullOrWhiteSpace(Text))
                return Vector2.Zero;
            
            var f = GetFont();

            switch (WrapMode)
            {
                case TextWrapMode.None:
                    _wrappedText = Text;
                    break;
                case TextWrapMode.LetterWrap:
                    _wrappedText = LetterWrap(f, Text, alottedSize.X);
                    break;
                case TextWrapMode.WordWrap:
                    _wrappedText = WordWrap(f, Text, alottedSize.X);
                    break;
            }

            var size = Vector2.Zero;
            foreach (var line in _wrappedText.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    size.Y += f.LineSpacing;
                    continue;
                }
                
                var m = f.MeasureString(line);

                size.X = Math.Max(size.X, m.X);
                size.Y += m.Y;
            }

            return size;
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            if (!string.IsNullOrWhiteSpace(_wrappedText))
            {
                var lines = _wrappedText.Split(Environment.NewLine);
                var pos = ContentRectangle.Location;
                
                var f = GetFont();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        pos.Y += f.LineSpacing;
                        continue;
                    }
                    
                    var m = f.MeasureString(line);
                    
                    switch (this.TextAlign)
                    {
                        case Gui.TextAlign.Left:
                            pos.X = ContentRectangle.Left;
                            break;
                        case Gui.TextAlign.Right:
                            pos.X = ContentRectangle.Right - m.X;
                            break;
                        case Gui.TextAlign.Center:
                            pos.X = ContentRectangle.Left + ((ContentRectangle.Width - m.X) / 2);
                            break;
                    }
                    
                    renderer.DrawString(f, line, pos, Color.GetColor(GuiSystem.Style.DefaultForeground), TextAlign);
                    pos.Y += m.Y;
                }
            }
        }
    }
}