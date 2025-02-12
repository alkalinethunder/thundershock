﻿using Thundershock.Gui.Styling;
using Thundershock.Core.Input;
using Thundershock.Core;


namespace Thundershock.Gui.Elements
{
    public class AdvancedButton : ContentElement, IButtonElement
    {
        private bool _isHovered;
        private bool _isPressed;
        
        public AdvancedButton()
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

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            GuiSystem.Style.DrawButton(renderer, this);
        }

    }
}