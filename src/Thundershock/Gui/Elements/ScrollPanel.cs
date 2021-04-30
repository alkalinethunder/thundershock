using System.Linq;
using Microsoft.Xna.Framework;
using Thundershock.Input;

namespace Thundershock.Gui.Elements
{
    public class ScrollPanel : Element
    {
        private int _scrollOffset;
        private int _pageHeight;
        private int _scrollHeight;

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            if (Children.Any())
            {
                // Measure the child with our alotted width but no limit on the height because we'll end up scrolling it.
                var childSize = Children.First().Measure(alottedSize);

                // We'll return that as our measurement - arrangement is when we deal with height limits.
                return childSize;
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
                var measure = Children.First().ActualSize;
                
                // Store the scroll height.
                _scrollHeight = (int) measure.Y - _pageHeight;
                
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
            var nextOffset = _scrollOffset + e.WheelDelta;
            
            // Clamp the offset
            if (nextOffset < 0) nextOffset = 0;
            if (nextOffset > _scrollHeight) nextOffset = _scrollHeight;

            return base.OnMouseScroll(e);
        }
    }
}