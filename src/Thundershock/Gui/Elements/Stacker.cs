using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Thundershock.Gui.Elements
{
    public class Stacker : Element
    {
        public static readonly string FillProperty = "Fill";
        
        public StackDirection Direction { get; set; } = StackDirection.Vertical;

        private float GetFillPercentage(Element elem)
        {
            if (elem.Properties.ContainsKey(FillProperty))
                return elem.Properties.GetValue<StackFill>(FillProperty).Percentage;
            return 0;
        }
        
        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            var sz = Vector2.Zero;

            foreach (var child in Children)
            {
                var m = child.Measure(alottedSize);

                sz = Direction switch
                {
                    StackDirection.Vertical => new Vector2(MathF.Max(sz.X, m.X), sz.Y + m.Y),
                    StackDirection.Horizontal => new Vector2(sz.X + m.X, MathF.Max(sz.Y, m.Y)),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            
            return sz;
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            // So this is how this works.
            //
            // First we need to know how big everything is, and we need to calculate our remaining space.
            //
            // Then based on that, we'll fill elements into the remaining space.
            //
            // And then we'll actually lay things out.
            var totalSpace = (Direction == StackDirection.Horizontal)
                ? contentRectangle.Width
                : contentRectangle.Height;
            var elements = new List<StackLayoutInfo>();

            foreach (var elem in Children)
            {
                var m = elem.ActualSize;

                var fill = GetFillPercentage(elem);

                var edata = new StackLayoutInfo();
                edata.Element = elem;
                edata.Size = (Direction == StackDirection.Horizontal) ? m.X : m.Y;
                edata.Fill = fill;

                elements.Add(edata);
            }
            
            // Figure out the total size of everything.
            var totalElemSize = elements.Aggregate(0f, (acc, x) => acc += x.Fill > 0 ? 0 : x.Size);
            
            // Remaining space.
            var remainingSpace = totalSpace - totalElemSize;
            
            // If this is greater than zero, we'll start dealing with filled elements.
            if (remainingSpace > 0)
            {
                var filledCount = elements.Count(x => x.Fill > 0);
                foreach (var elem in elements.Where(x => x.Fill > 0))
                {
                    // Calculate the segment.
                    var seg = remainingSpace / filledCount;
                    
                    // Segment is then multiplied by the fill amount.
                    seg *= elem.Fill;
                    
                    // And this becomes the element size.
                    elem.Size = seg;
                    
                    // And this space is now taken.
                    remainingSpace -= elem.Size;
                    filledCount--;
                }
            }
            
            // With filled elements dealt with, it's now time to organize things.
            var rect = contentRectangle;
            foreach (var elem in elements)
            {
                var m = elem.Element.ActualSize;

                if (Direction == StackDirection.Horizontal)
                {
                    m.X = elem.Size;
                    rect.Width = (int) m.X;
                }
                else
                {
                    m.Y = elem.Size;
                    rect.Height = (int) m.Y;
                }
                
                // position, my child!
                GetLayoutManager().SetChildBounds(elem.Element, rect);
                
                // You've been...
                //
                // THUNDERSTRUCK!
                if (Direction == StackDirection.Horizontal)
                {
                    rect.X += rect.Width;
                }
                else
                {
                    rect.Y += rect.Height;
                }
            }
        }

        private class StackLayoutInfo
        {
            public Element Element;
            public float Fill;
            public float Size;
        }
    }
}