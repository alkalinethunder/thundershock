using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Gui.Styling;
using Thundershock.Core.Input;

namespace Thundershock.Gui.Elements.Console
{
    public sealed class ConsoleControl : ContentElement, IConsole
    {
        public static readonly char BackgroundColorCode = '$';
        public static readonly char ForegroundColorCode = '#';
        public static readonly char AttributeCode = '&';
        
        private const double _cursorBlinkTime = 0.75;
        private const double _blinkTime = 1;

        private const int MaxLinesRetained = 5000;

        private int _linesWritten = 0;
        private double _blink;
        private double _cursorBlink;
        private bool _blinkShow = true;
        private bool _cursorShow = true;

        
        private bool _textIsDirty = true;
        private Attributes _attributes;
        private Font _regularFont;
        private Font _boldFont;
        private Font _italicFont;
        private Font _boldItalicFont;

        private ColorPalette _fallbackPalette = new ColorPalette();
        private ColorPalette _activePalette = null;

        private TextElement _hoverElement;
        
        private string _text = "";
        private string _input = string.Empty;
        private int _inputPos = 0;
        private float _textHeight;
        private float _inputHeight;
        private string[] _relevantCompletions = Array.Empty<string>();
        private int _activeCompletion = 0;
        private bool _inputIsDirty = false;
        private int _completionsPerPage = 10;
        private int _scrollbarWidth = 3;
        private float _scrollbackMax;
        private float _height;
        private float _scrollback;
        private Color _scrollFG = Color.Cyan;
        private Color _scrollBG = Color.Gray;
        private Color _highlight = Color.White;
        private Color _highlightFG = Color.Black;
        private Vector2 _completionY;
        private bool _paintCompletions;
        private float _completionsWidth;
        private int _completionPageStart;
        
        private const char CURSOR_SIGNAL = (char) 0xFF;

        private List<TextElement> _elements = new List<TextElement>();
        private List<TextElement> _inputElements = new List<TextElement>();

        // Font styling.
        public StyleFont BoldFont { get; set; } = StyleFont.Default;
        public StyleFont ItalicFont { get; set; } = StyleFont.Default;
        public StyleFont BoldItalicFont { get; set; } = StyleFont.Default;

        public bool DrawBackgroundImage { get; set; } = true;
        
        public IAutoCompleteSource AutoCompleteSource { get; set; }
        
        public ColorPalette ColorPalette
        {
            get => _activePalette ?? _fallbackPalette;
            set
            {
                _activePalette = value;
                _textIsDirty = true;
            }
        }

        public ConsoleControl()
        {
            CanFocus = true;
            IsInteractable = true;
        }

        private Color GetColor(ConsoleColor color)
        {
            return ColorPalette.GetColor(color);
        }

        private (int Start, int End, string Text) GetWordAtInputPos()
        {
            var end = 0;
            var start = 0;
            var word = string.Empty;

            if (_input.Length > 0)
            {
                for (var i = _inputPos; i >= 0; i--)
                {
                    if (i < _input.Length)
                    {
                        var ch = _input[i];
                        if (char.IsWhiteSpace(ch))
                            break;
                        start = i;
                    }
                }

                for (var i = _inputPos; i <= _input.Length; i++)
                {
                    if (i == _input.Length)
                    {
                        end = i;
                        break;
                    }
                    else
                    {
                        var ch = _input[i];
                        if (char.IsWhiteSpace(ch))
                            break;
                        end = i;
                    }
                }
            }
            else
            {
                start = 0;
                end = 0;
            }

            word = _input.Substring(start, end - start);
            
            return (start, end, word);
        }

        private IEnumerable<string> GetRelevantCompletions()
        {
            if (AutoCompleteSource != null)
            {
                var (start, end, word) = GetWordAtInputPos();
                if (start != end)
                {
                    foreach (var completion in AutoCompleteSource.GetCompletions(word))
                    {
                        if (completion.ToLower().StartsWith(word.ToLower()) && completion.Length > word.Length)
                            yield return completion;
                    }
                }
            }
        }
        
        public void ScrollToBottom()
        {
            _scrollback = 0;
        }
        
        private string[] BreakWords(string text)
        {
            var words = new List<string>();

            var word = string.Empty;
            foreach (var ch in text)
            {
                word += ch;
                if (char.IsWhiteSpace(ch))
                {
                    words.Add(word);
                    word = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(word))
                words.Add(word);
            
            return words.ToArray();
        }

        private string[] LetterWrap(Font font, string text, float width)
        {
            var lines = new List<string>();

            var line = string.Empty;
            var w = 0f;
            for (int i = 0; i <= text.Length; i++)
            {
                if (i < text.Length)
                {
                    var ch = text[i];
                    var m = font.MeasureString(ch.ToString()).X;
                    if (w + m >= width)
                    {
                        lines.Add(line);
                        line = "";
                        w = 0;
                    }

                    line += ch;
                    w += m;
                }
                else
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        lines.Add(line);
                    }
                }
            }
            
            return lines.ToArray();
        }

        public void MoveLeft(int amount)
        {
            if (_inputPos - amount < 0)
                _inputPos = 0;
            else
                _inputPos -= amount;
            _inputIsDirty = true;
        }

        private bool ParseColorCode(char code, out ConsoleColor color)
        {
            var result = true;
            
            int num = code switch
            {
                '0' => 0,
                '1' => 1,
                '2' => 2,
                '3' => 3,
                '4' => 4,
                '5' => 5,
                '6' => 6,
                '7' => 7,
                '8' => 8,
                '9' => 9,
                'a' => 10,
                'A' => 10,
                'b' => 11,
                'B' => 11,
                'c' => 12,
                'C' => 12,
                'd' => 13,
                'D' => 13,
                'e' => 14,
                'E' => 14,
                'f' => 15,
                'F' => 15,
                _ => 0
            };

            result = num > 0 || (num == 0 && code == '0');
            color = (ConsoleColor) num;
            return result;
        }

        public void MoveRight(int amount)
        {
            if (_inputPos + amount > _input.Length)
                _inputPos = _input.Length;
            else
                _inputPos += amount;
            _inputIsDirty = true;
        }

        public void MoveToHome()
        {
            _inputPos = 0;
            _inputIsDirty = true;
        }

        public void MoveToEnd()
        {
            _inputPos = _input.Length;
            _inputIsDirty = true;
        }

        public void ScrollUp(float amount)
        {
            if (_scrollback + amount > _scrollbackMax)
            {
                _scrollback = _scrollbackMax;
            }
            else
            {
                _scrollback += amount;
            }
        }

        public void ScrollDown(float amount)
        {
            _scrollback -= amount;
            if (_scrollback < 0)
                _scrollback = 0;
        }

        private void ApplyCompletion()
        {
            if (_relevantCompletions.Any())
            {
                var completion = _relevantCompletions[_activeCompletion];
                var (start, end, word) = GetWordAtInputPos();

                _input = _input.Remove(start, word.Length);
                _input = _input.Insert(start, completion);
                _inputPos = start;
                MoveRight(completion.Length + 1);
                _inputIsDirty = true;
            }
        }

        private Font GetFont(bool bold, bool italic)
        {
            // load default fonts if we lack them
            _regularFont ??= Core.Font.GetDefaultFont(GuiSystem.Graphics);
            _boldFont ??= Core.Font.GetDefaultFont(GuiSystem.Graphics);
            _boldItalicFont ??= Core.Font.GetDefaultFont(GuiSystem.Graphics);
            _italicFont ??= Core.Font.GetDefaultFont(GuiSystem.Graphics);

            if (bold && italic)
                return (BoldItalicFont ?? StyleFont.Default).GetFont(_boldItalicFont);
            else if (bold)
                return (BoldFont ?? StyleFont.Default).GetFont(_boldFont);
            else if (italic)
                return (ItalicFont ?? StyleFont.Default).GetFont(_italicFont);
            else
                return (Font ?? StyleFont.Default).GetFont(_regularFont);
        }

        private bool ParseAttribute(char code, out ConsoleAttribute attribute)
        {
            var attr = code switch
            {
                '0' => ConsoleAttribute.Reset,
                '1' => ConsoleAttribute.ResetFont,
                'u' => ConsoleAttribute.Underline,
                'i' => ConsoleAttribute.FontItalic,
                'b' => ConsoleAttribute.FontBold,
                'B' => ConsoleAttribute.FontNoBold,
                'I' => ConsoleAttribute.FontNoItalic,
                '2' => ConsoleAttribute.Blink,
                'U' => ConsoleAttribute.NoUnderline,
                'w' => ConsoleAttribute.WrapSet,
                'W' => ConsoleAttribute.WrapReset,
                _ => ConsoleAttribute.Unknown
            };

            attribute = attr;
            return attr != ConsoleAttribute.Unknown;
        }

        private void RegenTextElements()
        {
            // last attributes of non-input elements.
            var attrs = _attributes;
            
            // if text is dirty we're doing a full rerender.
            if (_textIsDirty)
            {
                // clear text elements
                _elements.Clear();
                
                // reset attributes
                attrs = new Attributes();
                attrs.Position = ContentRectangle.Location;
                attrs.Background = ConsoleColor.Black;
                attrs.Foreground = ConsoleColor.Gray;
                
                // Create text elements.
                CreateTextElements(ref attrs, _text, _elements, out _textHeight);
                
                // set the last attributes
                _attributes = attrs;
                
                _textIsDirty = false;
            }
            
            // handle dirty input
            if (_inputIsDirty)
            {
                // clear input elements
                _inputElements.Clear();
                
                // get input text
                var inputText = _input.Insert(_inputPos, CURSOR_SIGNAL.ToString()) + " ";
                
                // create text elements
                CreateTextElements(ref attrs, inputText, _inputElements, out _inputHeight);
                
                // Update completions.
                UpdateCompletions();

                // fix an autoscroll bug
                _inputHeight -= _regularFont.LineSpacing;
            }
            
            // Update height and scrollback info.
            _height = _textHeight + _inputHeight;
            
            // Oh and let's figure out what the max scrollback is.
            if (_height > ContentRectangle.Height)
            {
                _scrollbackMax = _height - ContentRectangle.Height;
            }
            else
            {
                _scrollbackMax = 0;
            }
        }

        private bool EscapeCharCode(string word, out int index)
        {
            var result = false;
            index = -1;

            for (var i = 0; i < word.Length; i++)
            {
                var ch = word[i];
                if (ch == BackgroundColorCode || ch == ForegroundColorCode || ch == AttributeCode)
                {
                    var next = i + 1;
                    if (next < word.Length)
                    {
                        var nextCh = word[next];
                        if (nextCh == ch)
                        {
                            index = i;
                            result = true;
                        }
                    }
                }
            }
            
            return result;
        }
        
        private void CreateTextElements(ref Attributes attrs, string rawText, List<TextElement> elements, out float elemHeight)
        {
            var rect = ContentRectangle; // Short hand for the terminal's bounding box. Needed for line wrapping.
            var firstLine = true; // line wrapping: have we not wrapped any line yet?
            
            // Account for the scrollbar width so the scrollbar never renders over text.
            rect.Width -= _scrollbarWidth;
            
            // Break the text we're given into words. The array that gets returned by BreakWords is independent of the font.
            // I just want to be able to break the text into whitespace-separated words without actually removing the whitespace
            // from the words. This makes word-wrapping A HELL of a lot easier.
            //
            // Also, don't expect this array to stay the same length. Later on when we get to color/formatting processing,
            // redterm will parse out format codes and break words accordingly.
            var outWords = BreakWords(rawText);
            
            // Now we get to actually creating text elements from these words. This is where color and formatting codes are
            // processed. We do not do word- or letter-wrapping at this stage in the process, because wrapping requires us to know
            // the font of each element already.
            for (var i = 0; i < outWords.Length; i++)
            {
                // Grab the next word to process. 
                var word = outWords[i];

                // Create the element for the word. This is where we assign the font, colors
                // and other attributes. Text isn't assigned yet.
                var elem = new TextElement();
                elem.Background = GetColor(attrs.Background);
                elem.Foreground = GetColor(attrs.Foreground);
                elem.Font = GetFont(attrs.Bold, attrs.Italic);
                elem.Underline = attrs.Underline;
                elem.Blinking = attrs.Blink;
                elem.IsWrapPoint = attrs.WrapSet;
                elem.IsWrapResetPoint = attrs.WrapReset;
                
                // Wrap set attrs are one-time values so they're reset after they've been applied to an element.
                attrs.WrapSet = false;
                attrs.WrapReset = false;
                
                // Attempt to find a double-sequence of either the Background, Foreground or Attribute format codes.
                // If we do then we're going to treat it as an escaped format code and only print one.
                if (EscapeCharCode(word, out int index))
                {
                    var next = index + 1;
                    var pre = word.Substring(0, next);
                    var post = word.Substring(next + 1);
                    word = pre;

                    Array.Resize(ref outWords, outWords.Length + 1);
                    outWords[i] = post;
                    for (var j = outWords.Length - 1; j > i; j--)
                    {
                        outWords[j] = outWords[j - 1];
                    }

                    outWords[i] = pre;
                }
                
                // Attempt to find a color/formatting code in the word. If one is found, the
                // index of the code in the word and a value representing the type of code found
                // will be passed through the out params.
                else if (FindFirstCharacterCode(word, out int colorCode, out ConsoleCode firstCode))
                {
                    // The index of the actual attribute char.
                    var colorCharIndex = colorCode + 1;
                    
                    // Is there actually an attribute char in the word?
                    if (colorCharIndex < word.Length)
                    {
                        // Misnomer: This is the ASCII representation of the attribute type to set.
                        var colorChar = word[colorCharIndex];
                        
                        // Check if we're dealing with a color code and try to parse the attr char as a console color.
                        // resulting color will be passed through out param.
                        if (firstCode != ConsoleCode.Attr && ParseColorCode(colorChar, out ConsoleColor color))
                        {
                            // This hellish code will break the word in half, parsing out the attribute we're handling.
                            // The words array will increase by 1 element and everything after the current word will be
                            // pushed by 1 index. The first half of current word will become our current word and the second
                            // half will become the next word.
                            var preWord = word.Substring(0, colorCharIndex - 1) ?? "";
                            var postWord = word.Substring(colorCharIndex + 1) ?? "";
                            word = preWord;
                            Array.Resize(ref outWords, outWords.Length + 1);
                            for (int j = outWords.Length - 1; j > i; j--)
                            {
                                outWords[j] = outWords[j - 1];
                            }

                            outWords[i + 1] = postWord;
                            outWords[i] = word;

                            // Set the correct color attribute for the next word.
                            if (firstCode == ConsoleCode.Bg)
                                attrs.Background = color;
                            else
                                attrs.Foreground = color;
                        }
                        
                        // Parse a non-color attribute. If we got a valid attribute, it'll be given to us
                        // via the out param.
                        else if (ParseAttribute(colorChar, out ConsoleAttribute attr))
                        {
                            // Same exact hellish code as the color code handler, for resizing and moving around
                            // the words array.
                            var preWord = word.Substring(0, colorCharIndex - 1) ?? "";
                            var postWord = word.Substring(colorCharIndex + 1) ?? "";
                            word = preWord;
                            Array.Resize(ref outWords, outWords.Length + 1);
                            for (int j = outWords.Length - 1; j > i; j--)
                            {
                                outWords[j] = outWords[j - 1];
                            }
                            outWords[i + 1] = postWord;
                            outWords[i] = word;

                            // Set the attributes of the next word.
                            switch (attr)
                            {
                                case ConsoleAttribute.Reset:
                                    attrs.Underline = false;
                                    attrs.Bold = false;
                                    attrs.Italic = false;
                                    attrs.Foreground = ConsoleColor.Gray;
                                    attrs.Background = ConsoleColor.Black;
                                    attrs.Blink = false;
                                    attrs.WrapSet = false;
                                    attrs.WrapReset = true;
                                    break;
                                case ConsoleAttribute.WrapSet:
                                    attrs.WrapSet = true;
                                    break;
                                case ConsoleAttribute.WrapReset:
                                    attrs.WrapReset = true;
                                    break;
                                case ConsoleAttribute.ResetFont:
                                    attrs.Bold = false;
                                    attrs.Italic = false;
                                    break;
                                case ConsoleAttribute.FontBold:
                                    attrs.Bold = true;
                                    break;
                                case ConsoleAttribute.FontItalic:
                                    attrs.Italic = true;
                                    break;
                                case ConsoleAttribute.FontNoBold:
                                    attrs.Bold = false;
                                    break;
                                case ConsoleAttribute.FontNoItalic:
                                    attrs.Italic = false;
                                    break;
                                case ConsoleAttribute.Underline:
                                    attrs.Underline = true;
                                    break;
                                case ConsoleAttribute.Blink:
                                    attrs.Blink = true;
                                    break;
                                case ConsoleAttribute.NoUnderline:
                                    attrs.Underline = false;
                                    break;
                            }
                        }
                    }
                }

                // Set the text of the element.
                elem.Text = word;
                
                // add the element to the output list.
                elements.Add(elem);
            }
            
            // TODO: This is where we handle the cursor... a huge optimization might involve removing this
            // entire loop and handling the cursor the same way we do the format codes....but...I'm too tired.
            for (int i = 0; i < elements.Count; i++)
            {
                // Current element to process.
                var elem = elements[i];

                // handle the cursor
                if (elem.Text.Contains(CURSOR_SIGNAL))
                {
                    var cursor = elem.Text.IndexOf(CURSOR_SIGNAL);

                    var cursorChar = cursor + 1;
                    var afterCursor = cursorChar + 1;

                    var text = elem.Text;

                    // this element gets everything before the cursor
                    elem.Text = text.Substring(0, cursor);

                    // this adds the cursor itself as a text element.
                    i++;
                    var cElem = new TextElement();
                    cElem.Font = elem.Font;
                    if (HasAnyFocus)
                    {
                        cElem.Background = ColorPalette.CursorColor;
                        cElem.Foreground = ColorPalette.CursorForeground;
                    }
                    else
                    {
                        cElem.Background = elem.Background;
                        cElem.Foreground = elem.Foreground;
                    }

                    cElem.IsCursor = true;
                    cElem.Underline = elem.Underline;
                    cElem.Text = text[cursorChar].ToString();
                    elements.Insert(i, cElem);

                    // and this is everything after the cursor.
                    i++;
                    var aElem = new TextElement();
                    aElem.Background = elem.Background;
                    aElem.Foreground = elem.Foreground;
                    aElem.Font = elem.Font;
                    aElem.Underline = elem.Underline;
                    aElem.Text = text.Substring(afterCursor);
                    elements.Insert(i, aElem);
                }
            }
            
            // Purge any elements without any text.
            for (int i = 0; i < elements.Count; i++)
            {
                var elem = elements[i];
                if (string.IsNullOrEmpty(elem.Text))
                {
                    elements.RemoveAt(i);
                    i--;
                }
            }
            
            // The next part of the process is positioning the now-processed elements.
            // This is where line wrapping happens, so do expect more elements to be created.
            var wrapPointAccount = 0f;
            for (int i = 0; i < elements.Count; i++)
            {
                // Current element to process.
                var elem = elements[i];

                // Handle wrap points.
                if (elem.IsWrapPoint)
                {
                    wrapPointAccount = attrs.Position.X - rect.Left;
                }
                else if (elem.IsWrapResetPoint)
                {
                    wrapPointAccount = 0;
                }
                
                // Measure the element.
                var measure = elem.Font.MeasureString(elem.Text);

                // wrap to new line if the measurement states we can't fit
                if (attrs.Position.X + measure.X >= rect.Right)
                {
                    attrs.Position.X = rect.Left + wrapPointAccount;
                    attrs.Position.Y += elem.Font.LineHeight;
                }

                // Set the position of the element from our attributes.
                elem.Position = attrs.Position;
                
                // is the element larger than a lie?
                if (measure.X >= rect.Width - wrapPointAccount)
                {
                    // letter-wrap the text
                    var lines = LetterWrap(elem.Font, elem.Text, rect.Width - wrapPointAccount);
                    
                    // this element gets the first line
                    elem.Text = lines.First();
                    
                    // re-measure
                    measure = elem.Font.MeasureString(elem.Text);
                    
                    // OH FUCKING JESUS FUCK FUCK
                    elem.MouseBounds = new Rectangle((int) elem.Position.X, (int) elem.Position.Y, (int) measure.X,
                        elem.Font.LineHeight);

                    // this is some seriously fucked shit
                    foreach (var line in lines.Skip(1))
                    {
                        // what the fuck?
                        i++;
                        
                        // oh my fucking god.
                        var wtf = elem.Clone();
                        wtf.Text = line;
                        
                        // I wanna die
                        wtf.Position.Y += elem.Font.LineHeight;
                        
                        // fuck this
                        attrs.Position = wtf.Position;
                        
                        // my god I'm screwed
                        elements.Insert(i, wtf);
                        elem = wtf;
                    
                        // SWEET MOTHER OF FUCK
                        measure = elem.Font.MeasureString(elem.Text);
                        
                        // OH FUCKING JESUS FUCK FUCK
                        elem.MouseBounds = new Rectangle((int) elem.Position.X, (int) elem.Position.Y, (int) measure.X,
                            elem.Font.LineHeight);
                    }
                }

                // store a rectangle for mouse hit detection
                elem.MouseBounds = new Rectangle((int) elem.Position.X, (int) elem.Position.Y, (int) measure.X,
                    elem.Font.LineHeight);
                
                // Go to a new line if the element ends with a new line.
                if (elem.Text.EndsWith('\n'))
                {
                    attrs.Position.X = rect.Left + wrapPointAccount;
                    attrs.Position.Y += elem.Font.LineHeight;
                }
                else
                {
                    attrs.Position.X += measure.X;
                }
            }

            // Now everything is positioned on screen so we're going to calculate
            // the height of everything. This will be factored into the terminal's scroll height.
            var lineY = -1f;
            var height = 0f;
            foreach (var elem in elements)
            {
                var y = elem.Position.Y;
                if (MathF.Abs(lineY - y) >= 0.00001f)
                {
                    lineY = y;
                    height += elem.MouseBounds.Height;
                }
            }

            // Report the element height to the caller.
            elemHeight = height;
        }

        private bool FindFirstCharacterCode(string word, out int firstIndex, out ConsoleCode firstCode)
        {
            var result = false;
            var index = 0;
            var code = ConsoleCode.Attr;
            
            for (var i = 0; i < word.Length; i++)
            {
                var ch = word[i];
                if (ch == BackgroundColorCode)
                {
                    index = i;
                    code = ConsoleCode.Bg;
                    result = true;
                    break;
                }

                if (ch == ForegroundColorCode)
                {
                    index = i;
                    code = ConsoleCode.Fg;
                    result = true;
                    break;
                }

                if (ch == AttributeCode)
                {
                    index = i;
                    code = ConsoleCode.Attr;
                    result = true;
                    break;
                }
            }

            firstIndex = index;
            firstCode = code;
            return result;
        }
        
        private void UpdateCompletions()
        {
            _relevantCompletions = GetRelevantCompletions().ToArray();
            _activeCompletion = 0;
            
            // find the cursor.
            var cursor = _inputElements.First(x => x.IsCursor);
            
            // is it the first element on the line?
            if (cursor.Position.X <= BoundingBox.Left)
            {
                // use this location as the auto-complete start position
                _completionY = cursor.Position;
            }
            else
            {
                // use the element before it.
                var index = _inputElements.IndexOf(cursor);
                if (index > 0)
                {
                    var elem = _inputElements[index - 1];
                    _completionY = elem.Position;
                }
                else
                {
                    _completionY = cursor.Position;
                }
            }

            _completionY.Y += cursor.Font.LineHeight;
            _paintCompletions = _relevantCompletions.Any();
            _completionPageStart = 0;
            
            if (_paintCompletions)
            {
                _completionsWidth = _relevantCompletions.Select(x => _regularFont.MeasureString(x + " ").X)
                    .OrderByDescending(x => x).First();
            }
        }
        
        protected override void OnUpdate(GameTime gameTime)
        {
            base.OnUpdate(gameTime);

            while (_linesWritten > MaxLinesRetained)
            {
                _text = _text.Substring(_text.IndexOf('\n') + 1);
                _linesWritten--;
                _textIsDirty = true;
            }
            
            _cursorBlink += gameTime.ElapsedGameTime.TotalSeconds;
            _blink += gameTime.ElapsedGameTime.TotalSeconds;

            if (_cursorBlink >= _cursorBlinkTime)
            {
                _cursorBlink = 0;
                _cursorShow = !_cursorShow;
            }

            if (_blink >= _blinkTime)
            {
                _blink = 0;
                _blinkShow = !_blinkShow;
            }

            if (_textIsDirty || _inputIsDirty)
            {
                RegenTextElements();
            }
        }

        protected override bool OnMouseScroll(MouseScrollEventArgs e)
        {
            var result = base.OnMouseScroll(e);

            var sb = _scrollback + (e.WheelDelta / 8);
            if (sb > _scrollbackMax)
                sb = _scrollbackMax;
            else if (sb < 0)
                sb = 0;
            _scrollback = sb;
            
            return result;
        }

        private bool PaintTextElementBackground(GameTime gameTime, GuiRenderer renderer, TextElement elem)
        {
            // use the mouse rect to speed things up.
            var rect = elem.MouseBounds;
            
            // Account for scrollback.
            rect.Y -= (int)_scrollbackMax;
            if (_height > ContentRectangle.Height)
            {
                rect.Y += (int)_scrollback;
            }

            if (rect.Bottom <= ContentRectangle.Top)
                return false;
            
            // only paint this element if it's above the bottom of the terminal bounds.
            if (rect.Top <= ContentRectangle.Bottom)
            {
                // Get the colors for the element.
                var bg = elem.Background;
                var fg = elem.Foreground;

                // Handle background images.
                if (bg.A < 255 && ColorPalette.BackgroundImage != null)
                    bg = Color.Transparent;

                // is this the cursor?
                if (elem.IsCursor)
                {
                    // is the cursor blinked off?
                    if (!_cursorShow)
                    {
                        // swap the colors of the cursor.
                        var s = bg;
                        bg = fg;
                        fg = s;
                    }
                }

                // if this is a blinking element, and the blinker is off, set foreground to transparent.
                if (elem.Blinking && !_blinkShow)
                {
                    fg = Color.Transparent;
                }

                // render the background rect followed by the text.
                renderer.FillRectangle(rect, bg);

                // Render a nice text underline if that attribute is set.
                if (elem.Underline)
                {
                    var h = rect.Height - 2;
                    rect.Y += h;
                    rect.Height = 2;
                    renderer.FillRectangle(rect, fg);
                }
            }

            return true;
        }

        private bool PaintTextElementText(GameTime gameTime, GuiRenderer renderer, TextElement elem)
        {
            // use the mouse rect to speed things up.
            var rect = elem.MouseBounds;
            
            // Account for scrollback.
            rect.Y -= (int)_scrollbackMax;
            if (_height > ContentRectangle.Height)
            {
                rect.Y += (int)_scrollback;
            }

            if (rect.Bottom <= ContentRectangle.Top)
                return false;
            
            // only paint this element if it's above the bottom of the terminal bounds.
            if (rect.Top <= ContentRectangle.Bottom)
            {
                // Get the colors for the element.
                var fg = elem.Foreground;
                
                // is this the cursor?
                if (elem.IsCursor)
                {
                    // is the cursor blinked off?
                    if (!_cursorShow)
                    {
                        // swap the colors of the cursor.
                        fg = elem.Background;
                    }
                }

                // if this is a blinking element, and the blinker is off, set foreground to transparent.
                if (elem.Blinking && !_blinkShow)
                {
                    return true;
                }

                // render the background rect followed by the text.
                renderer.DrawString(elem.Font, elem.Text, rect.Location, fg);
            }

            return true;
        }

        private void TryOpenUrl(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    var url = new Uri(text);

                    ThundershockPlatform.OpenFile(url.ToString());
                }
                catch
                {
                    // Ignored.
                }
            }
        }
        
        protected override bool OnMouseUp(MouseButtonEventArgs e)
        {
            var result = base.OnMouseUp(e);

            if (e.Button == MouseButton.Primary)
            {
                if (_hoverElement != null)
                {
                    TryOpenUrl(_hoverElement.Text);
                }

                result = true;
            }

            return result;
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            // It may be desirable for the game to not let us draw the background image specified in the
            // redterm palette. If this is the case, we won't - and we'll let the game itself decide exactly
            // how the background image is rendered. Some custom redwm layouts may not want to deal with
            // drawing the wallpaper, and so in that case, we will.
            if (ColorPalette.BackgroundImage != null && DrawBackgroundImage)
                renderer.FillRectangle(BoundingBox, ColorPalette.BackgroundImage, Color.White);
            
            renderer.FillRectangle(BoundingBox, GetColor(ConsoleColor.Black));
            
            var paintCompletionsThisTime = true;
            
            // PASS #1: Draw text element backgrounds.
            for (var i = _elements.Count - 1; i >= 0; i--)
            {
                var elem = _elements[i];
                if (!PaintTextElementBackground(gameTime, renderer, elem))
                    break;
            }
            
            for(var i = _inputElements.Count - 1; i >= 0; i--)
            {
                var elem = _inputElements[i];
                if (!PaintTextElementBackground(gameTime, renderer, elem))
                    break;
            }
            
            // paint the scroollbar.
            if (_scrollbarWidth > 0 && _height > BoundingBox.Height)
            {
                var sRect = BoundingBox;
                sRect.X = sRect.Right - _scrollbarWidth;
                sRect.Width = _scrollbarWidth;

                renderer.FillRectangle(sRect, _scrollBG);

                var height = BoundingBox.Height / _height;

                var pos = _height - BoundingBox.Height;
                pos -= _scrollback;

                sRect.Y = BoundingBox.Top + (int) ((pos / _height) * BoundingBox.Height);
                sRect.Height = (int) (BoundingBox.Height * height);
                renderer.FillRectangle(sRect, _scrollFG);
            }

            // PASS #2: Draw the actual text.
            for (var i = _elements.Count - 1; i >= 0; i--)
            {
                var elem = _elements[i];
                if (!PaintTextElementText(gameTime, renderer, elem))
                    break;
            }
            
            for(var i = _inputElements.Count - 1; i >= 0; i--)
            {
                var elem = _inputElements[i];
                if (!PaintTextElementText(gameTime, renderer, elem))
                    break;
            }

            if (paintCompletionsThisTime)
            {
                // Draw the completions menu
                if (_paintCompletions && IsFocused)
                {
                    var cHeight = Math.Min(_relevantCompletions.Length, _completionsPerPage) * _regularFont.LineSpacing;
                    var cPos = _completionY;

                    // Scrolling
                    cPos.Y -= _scrollbackMax;
                    if (_height > BoundingBox.Height)
                    {
                        cPos.Y += (int) _scrollback;
                    }

                    // Make sure the menu doesn't go beyond the width of the terminal
                    if (cPos.X + _completionsWidth > BoundingBox.Right)
                    {
                        var back = BoundingBox.Right - (cPos.X + _completionsWidth);
                        cPos.X -= back;
                    }

                    // if the page height's going to go below the terminal bounds then we're going to render above the cursor.
                    if (cPos.Y + cHeight > BoundingBox.Bottom)
                    {
                        // cursor
                        cPos.Y -= _regularFont.LineSpacing;
                        // menu
                        cPos.Y -= cHeight;
                    }

                    // paint the background
                    var bgRect = new Rectangle((int) cPos.X, (int) cPos.Y, (int) _completionsWidth, (int) cHeight);
                    renderer.FillRectangle(bgRect, ColorPalette.CompletionsBackground);

                    // paint each line
                    var c = 0;
                    bgRect.Height = _regularFont.LineSpacing;
                    for (int i = _completionPageStart; i < _relevantCompletions.Length; i++)
                    {
                        if (c > _completionsPerPage)
                            break;

                        c++;

                        // render the background if we're the active element
                        var color = ColorPalette.CompletionsText;
                        if (i == _activeCompletion)
                        {
                            renderer.FillRectangle(bgRect, ColorPalette.CompletionsHighlight);
                            color = ColorPalette.CompletionsHighlightText;
                        }

                        // draw the text
                        renderer.DrawString(_regularFont, _relevantCompletions[i], bgRect.Location, color);

                        bgRect.Y += _regularFont.LineSpacing;
                    }
                }
            }
        }

        protected override bool OnBlurred(FocusChangedEventArgs e)
        {
            base.OnBlurred(e);
            _inputIsDirty = true;
            return true;
        }

        protected override bool OnFocused(FocusChangedEventArgs e)
        {
            base.OnFocused(e);
            _inputIsDirty = true;
            return true;
        }

        protected override bool OnKeyDown(KeyEventArgs e)
        {
            var result = false;

            switch (e.Key)
            {
                case Keys.Left:
                    MoveLeft(1);
                    break;
                case Keys.Right:
                    MoveRight(1);
                    break;
                case Keys.Home:
                    MoveToHome();
                    break;
                case Keys.End:
                    MoveToEnd();
                    break;
                case Keys.Up:
                    if (_activeCompletion > 0)
                    {
                        _activeCompletion--;
                        if (_completionPageStart > _activeCompletion)
                        {
                            _completionPageStart--;
                        }
                    }

                    break;
                case Keys.Down:
                    if (_activeCompletion < _relevantCompletions.Length - 1)
                    {
                        _activeCompletion++;
                        if (_completionPageStart + _completionsPerPage < _activeCompletion)
                        {
                            _completionPageStart++;
                        }
                    }

                    break;
                case Keys.PageUp:
                    ScrollUp(BoundingBox.Height);
                    break;
                case Keys.PageDown:
                    ScrollDown(BoundingBox.Height);
                    break;
                case Keys.Delete:
                    if (_inputPos < _input.Length)
                    {
                        _input = _input.Remove(_inputPos, 1);
                        _inputIsDirty = true;
                    }

                    break;
                case Keys.Tab:
                    ApplyCompletion();
                    break;
                case Keys.Enter:
                    var nl = Environment.NewLine;
                    _input += nl;
                    _inputIsDirty = true;
                    MoveToEnd();
                    break;
                case Keys.Backspace:
                    if (_inputPos > 0)
                    {
                        _inputPos--;
                        _input = _input.Remove(_inputPos, 1);
                        _inputIsDirty = true;
                    }
                    break;
                default:
                    result = base.OnKeyDown(e);
                    break;
            }
            
            return result;
        }

        protected override bool OnKeyChar(KeyCharEventArgs e)
        {
            var result = base.OnKeyChar(e);

            ScrollToBottom();

            
            _input = _input.Insert(_inputPos, e.Character.ToString());
            _inputPos += 1;
            _inputIsDirty = true;
            result = true;

            return result;
        }

        protected override bool OnMouseMove(MouseMoveEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            var rect = BoundingBox;

            _hoverElement = null;
            
            for (var i = _elements.Count - 1; i >= 0; i--)
            {
                var elem = _elements[i];

                var elemRect = elem.MouseBounds;
                elemRect.Y -= (int) _scrollbackMax;
                
                if (_height > rect.Height)
                {
                    elemRect.Y += (int) _scrollback;
                }

                if (elemRect.Top >= rect.Bottom)
                    continue;

                if (elemRect.Bottom >= rect.Bottom)
                    break;

                if (x >= elemRect.Left && x <= elemRect.Right && y >= elemRect.Top && y <= elemRect.Bottom)
                {
                    _hoverElement = elem;
                    break;
                }
            }
            
            return base.OnMouseMove(e);
        }

        private class TextElement
        {
            public Font Font;
            public string Text;
            public Color Background;
            public Color Foreground;
            public bool Underline;
            public Vector2 Position;
            public bool IsCursor;
            public bool Blinking;
            public bool IsWrapPoint;
            public bool IsWrapResetPoint;
            public Rectangle MouseBounds;
            
            public TextElement Clone()
            {
                var elem = new TextElement();

                elem.Text = this.Text;
                elem.Position = this.Position;
                elem.Font = this.Font;
                elem.Background = this.Background;
                elem.Foreground = this.Foreground;
                elem.Blinking = this.Blinking;
                elem.Underline = this.Underline;

                return elem;
            }
        }

        private void CountLines(string text)
        {
            var sub = text;
            while (sub.Contains('\n'))
            {
                var i = sub.IndexOf('\n');
                sub = sub.Substring(i + 1);
                _linesWritten++;
            }
        }
        
        public void Write(object value)
        {
            var text = string.Empty;
            if (value == null)
                text = "null";
            else
                text = value.ToString();
            Write(text);
        }

        public void Write(string text)
        {
            _text += text;
            _textIsDirty = true;

            CountLines(text);
        }
        
        public void WriteLine(object value)
        {
            Write(value);
            WriteLine();
        }

        public void WriteLine(string text)
        {
            Write(text);
            WriteLine();
        }
        
        public void Write(string format, params object[] values)
        {
            var text = string.Format(format, values);
            Write(text);
        }

        public void WriteLine(string format, params object[] values)
        {
            Write(format, values);
            WriteLine();
        }

        public void WriteLine()
        {
            Write(Environment.NewLine);
        }

        public void Clear()
        {
            _linesWritten = 0;
            _text = string.Empty;
            _textIsDirty = true;
        }

        public bool GetLine(out string text)
        {
            // shorthand for newline character
            var nl = Environment.NewLine;
            
            // check if a full line of text has been entered.
            if (_input.Contains(nl))
            {
                // Get the first line of text in the input.
                // This also removes that line from the input.
                var index = _input.IndexOf(nl);
                text = _input.Substring(0, index);
                _input = _input.Substring(text.Length + nl.Length);
                _inputIsDirty = true;
                
                // Write the extracted line to output.
                WriteLine(text);
                
                // Move the cursor to the left by the removed amount of characters.
                MoveLeft(text.Length + nl.Length);
                
                return true;
            }

            text = string.Empty;
            return false;
        }

        public bool GetCharacter(out char character)
        {
            // do we have any input?
            if (!string.IsNullOrWhiteSpace(_input))
            {
                // get the first character.
                character = _input[0];
                
                // remove it from input.
                _input = _input.Substring(1);
                _inputIsDirty = true;
                
                // echo it
                Write(character);
                
                // Move the cursor to the left by 1.
                MoveLeft(1);

                return true;
            }

            character = '\0';
            return false;
        }

        private struct Attributes
        {
            public bool Bold;
            public bool Italic;
            public bool Underline;
            public bool Blink;
            public ConsoleColor Background;
            public ConsoleColor Foreground;
            public Vector2 Position;
            public bool WrapSet;
            public bool WrapReset;
        }
    }

    public enum ConsoleCode
    {
        Bg,
        Fg,
        Attr
    }
    
    public enum ConsoleAttribute
    {
        Unknown,
        Reset,
        ResetFont,
        FontBold,
        FontNoBold,
        FontItalic,
        FontNoItalic,
        Underline,
        NoUnderline,
        Blink,
        WrapSet,
        WrapReset
    }
}