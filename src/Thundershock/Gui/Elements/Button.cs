using Thundershock.Core;
using System.Numerics;
using Thundershock.Gui.Styling;
using Thundershock.Core.Input;

namespace Thundershock.Gui.Elements
{
    public class Button : Element, IButtonElement
    {
        private string _wrapped = string.Empty;
        private string _text = "Button Text";
        private bool _isPressed;
        private bool _isHovered;
        
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? string.Empty;
                    InvalidateMeasure();
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
            var font = Font.GetFont(GuiSystem.Style.ButtonFont);
            return font.MeasureString(Text);
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            var font = Font.GetFont(GuiSystem.Style.ButtonFont);

            var text = Text;
            var wrapped = TextBlock.WordWrap(font, text, contentRectangle.Width);

            _wrapped = wrapped;
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            var font = Font.GetFont(GuiSystem.Style.ButtonFont);

            var textColor = GuiSystem.Style.GetButtonTextColor(this);
            
            this.GuiSystem.Style.DrawButton(renderer, this);

            var lines = _wrapped.Split('\n');

            var y = ContentRectangle.Y;

            foreach (var line in lines)
            {
                var m = font.MeasureString(line);
                var x = ContentRectangle.Left + ((ContentRectangle.Width - m.X) / 2);

                renderer.DrawString(font, line, new Vector2(x, y), textColor);
                
                y += font.LineSpacing;
            }
        }
    }
}