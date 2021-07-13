using System;
using System.Numerics;
using Thundershock.Core;

namespace Thundershock.Gui.Elements
{
    public class WrapPanel : LayoutElement
    {
        private StackDirection _orientation = StackDirection.Horizontal;
        
        public StackDirection Orientation
        {
            get => _orientation;
            set => _orientation = value;
        }

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            var size = Vector2.Zero;

            var max = float.PositiveInfinity;

            switch (Orientation)
            {
                case StackDirection.Horizontal:
                    if (alottedSize.X > 0)
                        max = alottedSize.X;
                    break;
                case StackDirection.Vertical:
                    if (alottedSize.Y > 0)
                        max = alottedSize.Y;
                    break;
            }

            var line = 0f;
            var lineSize = 0f;
            
            foreach (var elem in Children)
            {
                var elemSize = elem.Measure(alottedSize);

                switch (Orientation)
                {
                    case StackDirection.Horizontal:
                        if (lineSize + elemSize.X > max)
                        {
                            size.X = Math.Max(size.X, lineSize);
                            size.Y += line;
                            line = 0;
                            lineSize = 0;
                        }

                        lineSize += elemSize.X;
                        line = Math.Max(line, elemSize.Y);
                        break;
                    case StackDirection.Vertical:
                        if (lineSize + elemSize.Y > max)
                        {
                            size.X += line;
                            size.Y = Math.Max(lineSize, size.Y);
                            line = 0;
                            lineSize = 0;
                        }

                        line = Math.Max(line, elemSize.X);
                        
                        lineSize += elemSize.Y;
                        
                        break;
                }
            }

            if (line > 0)
            {
                switch (Orientation)
                {
                    case StackDirection.Horizontal:
                        size.Y += line;
                        break;
                    case StackDirection.Vertical:
                        size.X += line;
                        break;
                }
            }

            if (lineSize > 0)
            {
                switch (Orientation)
                {
                    case StackDirection.Horizontal:
                        size.X = Math.Max(lineSize, size.X);
                        break;
                    case StackDirection.Vertical:
                        size.Y = Math.Max(lineSize, size.Y);
                        break;
                }
            }
            
            return size;
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            var x = ContentRectangle.Left;
            var y = ContentRectangle.Top;
            var line = 0f;
            foreach (var elem in Children)
            {
                var size = elem.ActualSize;

                var rect = Rectangle.Empty;
                rect.Width = (int) size.X;
                rect.Height = (int) size.Y;

                switch (Orientation)
                {
                    case StackDirection.Horizontal:
                        if (x + size.X > contentRectangle.Right)
                        {
                            x = contentRectangle.Left;
                            y += line;
                            line = 0;
                        }
                        line = Math.Max(line, size.Y);
                        rect.X = (int) x;
                        rect.Y = (int) y;
                        x += size.X;
                        break;
                    case StackDirection.Vertical:
                        if (y + size.Y > contentRectangle.Bottom)
                        {
                            y = contentRectangle.Top;
                            x += line;
                            line = 0;
                        }
                        line = Math.Max(line, size.X);
                        rect.X = (int) x;
                        rect.Y = (int) y;
                        y += size.Y;
                        break;

                }

                GetLayoutManager().SetChildBounds(elem, rect);
            }
        }
    }
}
