using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thundershock.Gui.Elements
{
    public class WrapPanel : Element
    {
        public StackDirection Orientation { get; set; } = StackDirection.Horizontal;

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            var size = Vector2.Zero;

            foreach (var elem in Children)
            {
                var elemSize = elem.Measure(alottedSize);

                switch (Orientation)
                {
                    case StackDirection.Horizontal:
                        size.X += elemSize.X;
                        size.Y = Math.Max(size.Y, elemSize.Y);
                        break;
                    case StackDirection.Vertical:
                        size.X = Math.Max(size.X, elemSize.X);
                        size.Y += elemSize.Y;
                        break;
                }
            }

            return size;
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            var x = (float) ContentRectangle.Left;
            var y = (float) ContentRectangle.Top;
            var line = 0f;
            foreach (var elem in Children)
            {
                var size = elem.Measure(ContentRectangle.Size.ToVector2());

                var rect = Rectangle.Empty;
                rect.Width = (int) size.X;
                rect.Height = (int) size.Y;

                switch (Orientation)
                {
                    case StackDirection.Horizontal:
                        if (x + size.X >= contentRectangle.Right)
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
                        if (y + size.Y >= contentRectangle.Bottom)
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
