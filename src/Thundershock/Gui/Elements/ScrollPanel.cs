using System;
using System.Linq;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Input;

namespace Thundershock.Gui.Elements
{
    public class ScrollPanel : Element
    {
        private float _scrollOffset;
        private float _pageHeight;
        private float _scrollHeight;

        public ScrollPanel()
        {
            IsInteractable = true;
        }
        
        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            if (Children.Any())
            {
                // Measure the child's size using our alotted width and an infinite height.
                var childSize = Children.First().Measure(new Vector2(alottedSize.X, 0));
                
                // If we don't have an alotted height then return what we just measured.
                if (alottedSize.Y <= 0)
                    return childSize;
                
                // otherwise return the child width and whichever height is smaller.
                // This fixes a layout bug with scroll panels that don't have a MaximumHeight.
                return new Vector2(childSize.X, Math.Min(alottedSize.Y, childSize.Y));
            }

            return Vector2.Zero;
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            // So...the intention is that our parent constrained our height.
            // And that's how we'll deal with scrolling.
            // So first we need to store the page height.
            _pageHeight = contentRectangle.Height;
            
            // And now we deal with our child.
            if (Children.Any())
            {
                var measure = Children.First().Measure(contentRectangle.Size);
                
                // Store the scroll height.
                _scrollHeight = (int) measure.Y;
                
                // Clamp the scroll offset.
                if (_scrollOffset >= _scrollHeight)
                {
                    _scrollOffset = _scrollHeight;
                }

                if (_scrollHeight < _pageHeight)
                {
                    _scrollOffset = 0;
                }
                
                // Perform layout.
                GetLayoutManager().SetChildBounds(Children.First(), new Rectangle(contentRectangle.Left, contentRectangle.Top - _scrollOffset, contentRectangle.Width, (int) measure.Y));
            }
        }

        protected override bool OnMouseScroll(MouseScrollEventArgs e)
        {
            var nextOffset = _scrollOffset - (e.WheelDelta / 4);
            
            // Clamp the offset
            if (nextOffset > _scrollHeight - _pageHeight) nextOffset = _scrollHeight - _pageHeight;
            if (nextOffset < 0) nextOffset = 0;

            _scrollOffset = nextOffset;

            return base.OnMouseScroll(e);
        }
    }
}