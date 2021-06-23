using System;
using System.Numerics;
using Thundershock.Core;

namespace Thundershock.Gui.Elements
{
    public class FreePanel : Element
    {
        public static readonly string AnchorProperty = "Anchor";
        public static readonly string AlignmentProperty = "Alignment";
        public static readonly string PositionProperty = "Position";
        public static readonly string AutoSizeProperty = "AutoSize";
        public static readonly string SizeProperty = "Size";

        public CanvasAnchor DefaultAnchor { get; set; }
        public bool DefaultAutoSize { get; set; }
        public Vector2 DefaultAlignment { get; set; }
        
        public struct CanvasAnchor
        {
            public float Left;
            public float Top;
            public float Right;
            public float Bottom;

            public CanvasAnchor(float left, float top, float right, float bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public static CanvasAnchor Fill => new(0, 0, 1, 1);
            public static CanvasAnchor TopSide => new(0, 0, 1, 0);
            public static CanvasAnchor LeftSide => new(0, 0, 0, 1);
            public static CanvasAnchor RightSide => new(1, 0, 1, 1);
            public static CanvasAnchor BottomSide => new(0, 1, 1, 1);
            public static CanvasAnchor Horizontal => new(0, 0.5f, 1, 0.5f);
            public static CanvasAnchor Vertical => new(0.5f, 0, 9.5f, 1);
            public static CanvasAnchor Center => new(0.5f, 0.5f, 0, 0);
            public static CanvasAnchor TopLeft => new(0, 0, 0, 0);
        }

        private bool GetAutoSize(Element elem)
        {
            if (elem.Properties.ContainsKey(AutoSizeProperty))
                return elem.Properties.GetValue<bool>(AutoSizeProperty);
            return DefaultAutoSize;
        }
        
        private CanvasAnchor GetAnchor(Element elem)
        {
            if (elem.Properties.ContainsKey(AnchorProperty))
                return elem.Properties.GetValue<CanvasAnchor>(AnchorProperty);
            return DefaultAnchor;
        }

        private Vector2 GetAlignment(Element elem)
        {
            if (elem.Properties.ContainsKey(AlignmentProperty))
                return elem.Properties.GetValue<Vector2>(AlignmentProperty);
            return DefaultAlignment;
        }

        public Vector2 GetPosition(Element element)
        {
            if (element.Properties.ContainsKey(PositionProperty))
                return element.Properties.GetValue<Vector2>(PositionProperty);

            return Vector2.Zero;
        }

        public Vector2 GetSize(Element elem)
        {
            if (elem.Properties.ContainsKey(SizeProperty))
                return elem.Properties.GetValue<Vector2>(SizeProperty);
            return Vector2.Zero;
        }

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            var size = Vector2.Zero;

            foreach (var elem in Children)
            {
                var s = Vector2.Zero;
                if (GetAutoSize(elem))
                {
                    s = GetSize(elem);
                }
                else
                {
                    s = elem.Measure(alottedSize);
                }
                
                size.X = MathF.Max(size.X, s.X);
                size.Y = MathF.Max(size.Y, s.Y);
            }
            
            return size;
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            foreach (var elem in Children)
            {
                var size = Vector2.Zero;
                if (GetAutoSize(elem))
                {
                    size = elem.Measure();
                }
                else
                {
                    size = GetSize(elem);
                }

                var anchor = GetAnchor(elem);
                var align = GetAlignment(elem);
                
                var position = new Vector2(contentRectangle.Left + (contentRectangle.Width * anchor.Left),
                    contentRectangle.Top + (contentRectangle.Height * anchor.Top));

                position -= (size * align);

                position += GetPosition(elem);

                var rect = new Rectangle(position.X, position.Y, 0, 0);

                rect.Width = (contentRectangle.Width * anchor.Right);
                rect.Height = (contentRectangle.Height * anchor.Bottom);

                if (rect.Width <= 0) rect.Width = size.X;
                if (rect.Height <= 0) rect.Height = size.Y;
                
                GetLayoutManager().SetChildBounds(elem, rect);
            }
        }
    }
}