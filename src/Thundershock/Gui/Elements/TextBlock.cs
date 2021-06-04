using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Thundershock.Gui.Elements
{
    public class TextBlock : Element
    {
        private string _wrappedText;
        
        public Color Color { get; set; } = Color.Black;
        public string Text { get; set; } = "Text Block";
        public SpriteFont Font { get; set; }

        public TextAlign TextAlign { get; set; }
        
        public TextWrapMode WrapMode { get; set; }
        
        private SpriteFont GetFont()
        {
            return Font ?? GuiSystem.Style.DefaultFont;
        }

        public static string LetterWrap(SpriteFont font, string text, float wrapWidth)
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

        public static string WordWrap(SpriteFont font, string text, float wrapWidth)
        {
            if (wrapWidth <= 0)
                return text;
            
            // first step: break words.
            var words = new List<string>();
            var w = "";
            for (var i = 0; i <= text.Length; i++)
            {
                if (i < text.Length)
                {
                    var ch = text[i];
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
            var sb = new StringBuilder();
            var lineWidth = 0f;
            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                var m = font.MeasureString(word).X;
                var m2 = font.MeasureString(word.Trim()).X;
                
                if (lineWidth + m2 > wrapWidth && lineWidth > 0) // this makes the whole thing a lot less greedy
                {
                    sb.AppendLine();
                    lineWidth = 0;
                }

                if (m > lineWidth)
                {
                    var letterWrapped = LetterWrap(font, word, wrapWidth);
                    var lines = letterWrapped.Split(Environment.NewLine);
                    var last = lines.Last();

                    m = font.MeasureString(last).X;
                    word = last;

                    sb.Append(letterWrapped);
                }
                else
                {
                    sb.Append(word);
                }

                if (word.EndsWith('\n'))
                    lineWidth = 0;
                else
                    lineWidth += m;
            }
            
            
            return sb.ToString();
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

            return f.MeasureString(_wrappedText);
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            if (!string.IsNullOrWhiteSpace(_wrappedText))
            {
                var lines = _wrappedText.Split(Environment.NewLine);
                var pos = ContentRectangle.Location.ToVector2();
                
                var f = GetFont();

                foreach (var line in lines)
                {
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
                    
                    renderer.DrawString(f, line, pos, Color, TextAlign);
                    pos.Y += f.LineSpacing;
                }
            }
        }
    }
}