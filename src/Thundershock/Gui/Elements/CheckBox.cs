using System;
using Microsoft.Xna.Framework;
using Thundershock.Input;

namespace Thundershock.Gui.Elements
{
    public class CheckBox : Element
    {
        private CheckState _checkState = CheckState.Unchecked;
        private bool _isHovered;

        public CheckBox()
        {
            CanFocus = true;
            IsInteractable = true;
        }    
            
        public CheckState CheckState
        {
            get => _checkState;
            set
            {
                if (_checkState != value)
                {
                    _checkState = value;
                    CheckStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsChecked
        {
            get => _checkState == CheckState.Checked;
            set => CheckState = value ? CheckState.Checked : CheckState.Unchecked;
        }
        
        public event EventHandler CheckStateChanged;

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            var m = base.MeasureOverride(alottedSize);

            m.X += GuiSystem.Style.CheckSize;
            m.Y = Math.Max(m.Y, GuiSystem.Style.CheckSize);
            
            return m;
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            contentRectangle.X += GuiSystem.Style.CheckSize;
            contentRectangle.Width -= GuiSystem.Style.CheckSize;
            
            base.ArrangeOverride(contentRectangle);
        }

        protected override bool OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Primary)
            {
                CheckState = IsChecked ? CheckState.Unchecked : CheckState.Checked;
            }
            
            return base.OnMouseUp(e);
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            var bounds = ContentRectangle;

            bounds.Width = GuiSystem.Style.CheckSize;
            bounds.Height = bounds.Width;
            bounds.Y = ContentRectangle.Top + ((ContentRectangle.Height - bounds.Height) / 2);

            GuiSystem.Style.DrawCheckBox(renderer, bounds, _checkState, _isHovered);
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
    }
}