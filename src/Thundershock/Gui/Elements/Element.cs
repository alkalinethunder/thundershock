using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Numerics;
using Gtk;
using Thundershock.Flumberboozles;
using Thundershock.Gui.Styling;
using Thundershock.Core.Input;
using Thundershock.Core;

namespace Thundershock.Gui.Elements
{
    public abstract class Element : IPropertySetOwner
    {
        private bool _isRenderDataDirty = true;
        private float _computedOpacity;
        private Color _computedTint;
        private GuiSystem _guiSystem;
        private float _opacity = 1;
        private float _minWidth;
        private float _minHeight;
        private float _maxWidth;
        private float _maxHeight;
        private float _fixedWidth;
        private float _fixedHeight;
        private float _widthUnitRounding;
        private float _heightUnitRounding;
        private string _name;
        private PropertySet _props;
        private StyleFont _font = StyleFont.Default;
        private Visibility _visibility = Visibility.Visible;
        private Padding _padding;
        private Padding _margin;
        private LayoutManager _layout;
        private Element _parent;
        private ElementCollection _children;
        private HorizontalAlignment _hAlign;
        private VerticalAlignment _vAlign;
        private Rectangle _bounds;
        private Rectangle _contentRect;
        private Rectangle _clipRect;

        public Rectangle ClipBounds => _clipRect;

        public float ComputedOpacity => _computedOpacity;
        public Color ComputedTint => _computedTint;
        
        public bool CanFocus { get; set; }
        
        public Rectangle ContentRectangle => _contentRect;
        
        public bool IsInteractable { get; set; }
        
        public string ToolTip { get; set; }

        public StyleFont Font
        {
            get => _font;
            set
            {
                if (_font != value)
                {
                    _font = value;
                }
            }
        }

        public PropertySet Properties => _props;

        public float WidthUnitRounding
        {
            get => _widthUnitRounding;
            set => _widthUnitRounding = value;
        }
        
        public GuiSystem GuiSystem
            => _guiSystem;
        
        public Rectangle BoundingBox
            => _bounds;

        private string DefaultName
            => $"{this.GetType().Name}_{GetHashCode()}";
        
        public string Name
        {
            get => _name;
            set => _name = value ?? DefaultName;
        }
        
        public HorizontalAlignment HorizontalAlignment
        {
            get => _hAlign;
            set
            {
                if (_hAlign != value)
                {
                    _hAlign = value;
                }
            }
        }

        public VerticalAlignment VerticalAlignment
        {
            get => _vAlign;
            set
            {
                if (_vAlign != value)
                {
                    _vAlign = value;
                }
            }
        }

        public float Opacity
        {
            get => _opacity;
            set
            {
                value = MathHelper.Clamp(value, 0, 1);
                
                if (Math.Abs(_opacity - value) > 0.0001f)
                {
                    _opacity = value;
                    InvalidateRenderData();
                }
            }
        }
        
        public abstract bool CanPaint { get; }
        
        public bool Enabled { get; set; } = true;

        public StyleColor ForeColor { get; set; } = StyleColor.Default;
        
        public Element Parent
        {
            get => _parent;
            set => _parent = value;
        }

        protected virtual bool SupportsChildren => false;

        public ElementCollection Children => _children;

        public bool IsFocused => GuiSystem.FocusedElement == this;
        public bool HasAnyFocus => IsFocused || _children.Any(x => x.HasAnyFocus);

        public Padding Padding
        {
            get => _padding;
            set
            {
                if (_padding != value)
                {
                    _padding = value;
                }
            }
        }

        public Padding Margin
        {
            get => _margin;
            set
            {
                if (_margin != value)
                {
                    _margin = value;
                }
            }
        }
        
        public float FixedWidth
        {
            get => _fixedWidth;
            set
            {
                if (MathF.Abs(_fixedWidth - value) >= 0.0001f)
                {
                    _fixedWidth = value;
                }
            }
        }

        public float FixedHeight
        {
            get => _fixedHeight;
            set
            {
                if (MathF.Abs(_fixedHeight - value) >= 0.0001f)
                {
                    _fixedHeight = value;
                }
            }
        }

        public float MinimumWidth
        {
            get => _minWidth;
            set
            {
                if (MathF.Abs(_minWidth - value) >= 0.0001f)
                {
                    _minWidth = value;
                }
            }
        }

        public float MinimumHeight
        {
            get => _minHeight;
            set
            {
                if (MathF.Abs(_minHeight - value) >= 0.0001f)
                {
                    _minHeight = value;
                }
            }
        }

        public Visibility Visibility
        {
            get => _visibility;
            set
            {
                if (_visibility != value)
                {
                    _visibility = value;
                }
            }
        }
        
        public float MaximumWidth
        {
            get => _maxWidth;
            set
            {
                if (MathF.Abs(_maxWidth - value) >= 0.0001f)
                {
                    _maxWidth = value;
                }
            }
        }

        public float MaximumHeight
        {
            get => _maxHeight;
            set
            {
                if (MathF.Abs(_maxHeight - value) >= 0.0001f)
                {
                    _maxHeight = value;
                }
            }
        }
        
        public Vector2 ActualSize { get; private set; }
        
        public Element()
        {
            _children = new ElementCollection(this);
            _layout = new LayoutManager(this);
            _name = DefaultName;

            _props = new(this);
        }

        protected void SetGuiSystem(GuiSystem gui)
        {
            if (_guiSystem != null)
                throw new InvalidOperationException("You can only set the GuiSystem of the Root Element.");

            _guiSystem = gui;
        }
        
        protected virtual void ArrangeOverride(Rectangle contentRectangle)
        {
            foreach (var child in Children)
            {
                child.GetLayoutManager().SetBounds(contentRectangle);
            }
        }
        
        protected virtual Vector2 MeasureOverride(Vector2 alottedSize)
        {
            var size = Vector2.Zero;

            foreach (var child in Children)
            {
                var childSize = child._layout.GetContentSize(alottedSize);
                size.X = MathF.Max(size.X, childSize.X);
                size.Y = MathF.Max(size.Y, childSize.Y);
            }
            
            return size;
        }
        
        protected LayoutManager GetLayoutManager()
        {
            return _layout;
        }

        public void Update(GameTime gameTime)
        {
            OnUpdate(gameTime);
            foreach (var child in Children)
            {
                child.Update(gameTime);
            }
        }

        public void Paint(GameTime gameTime, GuiRenderer renderer)
        {
            OnPaint(gameTime, renderer);
        }

        public Vector2 Measure(Vector2 alottedSize = default)
        {
            // If the UI element is collapsed, report a measurement of zero.
            if (Visibility == Visibility.Collapsed)
            {
                // this fixes a layout bug!
                ActualSize = Vector2.Zero;
                return Vector2.Zero;
            }

            alottedSize.X -= Margin.Width;
            alottedSize.Y -= Margin.Height;
            alottedSize.X -= Padding.Width;
            alottedSize.Y -= Padding.Height;
            
            // This forces the alotted size to fit our maximum size.
            if (_maxWidth > 0)
                alottedSize.X = _maxWidth;

            if (_maxHeight > 0)
                alottedSize.Y = _maxHeight;
            
            var measure = this.MeasureOverride(alottedSize);

            if (_fixedWidth > 0)
                measure.X = _fixedWidth;

            if (_fixedHeight > 0)
                measure.Y = _fixedHeight;
                
            if (_minWidth > 0)
                measure.X = MathF.Max(_minWidth, measure.X);

            if (_minHeight > 0)
                measure.Y = MathF.Max(_minHeight, measure.Y);

            if (_maxWidth > 0)
                measure.X = MathF.Min(_maxWidth, measure.X);

            if (_maxHeight > 0)
                measure.Y = MathF.Min(_maxHeight, measure.Y);

            // Apply width and height clamping.
            if (_widthUnitRounding > 0)
            {
                measure.X = MathF.Ceiling(measure.X / _widthUnitRounding) * _widthUnitRounding;
            }
            if (_heightUnitRounding > 0)
            {
                measure.Y = MathF.Ceiling(measure.Y / _heightUnitRounding) * _heightUnitRounding;
            }

            measure += Margin.Size + Padding.Size;
            
            ActualSize = measure;
            
            return measure;
        }
        
        protected virtual void OnPaint(GameTime gameTime, GuiRenderer renderer) {}
        protected virtual void OnUpdate(GameTime gameTime) {}
        protected virtual void OnPaint(GuiRenderer renderer) {}

        public class LayoutManager
        {
            private Element _owner;
            
            public Vector2 GetContentSize(Vector2 alottedSize = default)
            {
                return _owner.Measure(alottedSize);
            }
            
            public LayoutManager(Element element)
            {
                _owner = element;
            }

            public void SetChildBounds(Element elem, Rectangle rect)
            {
                elem.GetLayoutManager().SetBounds(rect);
            }

            public void SetBounds(Rectangle rectangle)
            {
                if (_owner.BoundingBox != rectangle)
                {
                    var contentSize = this.GetContentSize(rectangle.Size);

                    // Apply padding.
                    rectangle.X += _owner.Padding.Left;
                    rectangle.Y += _owner.Padding.Top;
                    rectangle.Width -= _owner.Padding.Width;
                    rectangle.Height -= _owner.Padding.Height;

                    var bounds = Rectangle.Empty;

                    switch (_owner.HorizontalAlignment)
                    {
                        case HorizontalAlignment.Center:
                            bounds.Width = (int) Math.Round(contentSize.X);
                            bounds.X = rectangle.Left + ((rectangle.Width - bounds.Width) / 2);
                            break;
                        case HorizontalAlignment.Left:
                            bounds.Width = (int) Math.Round(contentSize.X);
                            bounds.X = rectangle.Left;
                            break;
                        case HorizontalAlignment.Right:
                            bounds.Width = (int) Math.Round(contentSize.X);
                            bounds.X = rectangle.Right - bounds.Width;
                            break;
                        case HorizontalAlignment.Stretch:
                            bounds.Width = rectangle.Width;
                            bounds.X = rectangle.Left;
                            break;
                    }

                    switch (_owner.VerticalAlignment)
                    {
                        case VerticalAlignment.Center:
                            bounds.Height = (int) Math.Round(contentSize.Y);
                            bounds.Y = rectangle.Top + ((rectangle.Height - bounds.Height) / 2);
                            break;
                        case Gui.VerticalAlignment.Top:
                            bounds.Height = (int) Math.Round(contentSize.Y);
                            bounds.Y = rectangle.Top;
                            break;
                        case VerticalAlignment.Bottom:
                            bounds.Height = (int) Math.Round(contentSize.Y);
                            bounds.Y = rectangle.Bottom - bounds.Height;
                            break;
                        case VerticalAlignment.Stretch:
                            bounds.Height = rectangle.Height;
                            bounds.Y = rectangle.Top;
                            break;
                    }

                    _owner._bounds = bounds;

                    // Margins
                    bounds.X += _owner.Margin.Left;
                    bounds.Y += _owner.Margin.Top;
                    bounds.Width -= _owner.Margin.Width;
                    bounds.Height -= _owner.Margin.Height;

                    _owner._contentRect = bounds;
                    
                    _owner._clipRect = ComputeClipRect();
                }
                
                _owner.ArrangeOverride(_owner.ContentRectangle);
            }

            private Rectangle ComputeClipRect()
            {
                var e = _owner;
                var rect = e.BoundingBox;
                while (e != null)
                {
                    rect = Rectangle.Intersect(e.BoundingBox, rect);
                    e = e.Parent;
                }

                var loc = rect.Location;
                var size = rect.Size + Vector2.One;

                size = _owner.GuiSystem.ViewportToScreen(size);
                loc = _owner.GuiSystem.ViewportToScreen(loc);

                rect.X = loc.X;
                rect.Y = loc.Y;
                rect.Width = size.X;
                rect.Height = size.Y;

                return rect;
            }
        }

        public bool HasParent(Element element)
        {
            var p = Parent;

            while (p != null)
            {
                if (p == element)
                    return true;

                p = p.Parent;
            }
            
            return false;
        }

        public event EventHandler<FocusChangedEventArgs> Blurred;
        public event EventHandler<FocusChangedEventArgs> Focused;
        public event EventHandler<KeyCharEventArgs> KeyChar;
        public event EventHandler<MouseScrollEventArgs> MouseScroll;
        public event EventHandler<KeyEventArgs> KeyDown;
        public event EventHandler<KeyEventArgs> KeyUp;
        public event EventHandler<MouseMoveEventArgs> MouseMove;
        public event EventHandler<MouseButtonEventArgs> MouseDown;
        public event EventHandler<MouseButtonEventArgs> MouseUp;
        public event EventHandler<MouseMoveEventArgs> MouseEnter;
        public event EventHandler<MouseMoveEventArgs> MouseLeave;

        protected virtual bool OnMouseEnter(MouseMoveEventArgs e)
        {
            if (MouseEnter != null)
            {
                MouseEnter(this, e);
                return true;
            }

            return false;
        }
        
        protected virtual bool OnMouseLeave(MouseMoveEventArgs e)
        {
            if (MouseLeave != null)
            {
                MouseLeave(this, e);
                return true;
            }

            return false;
        }
        
        protected virtual bool OnMouseDown(MouseButtonEventArgs e)
        {
            if (MouseDown != null)
            {
                MouseDown(this, e);
                return true;
            }

            return false;
        }
        
        protected virtual bool OnMouseUp(MouseButtonEventArgs e)
        {
            if (MouseUp != null)
            {
                MouseUp(this, e);
                return true;
            }

            return false;
        }
        
        protected virtual bool OnMouseMove(MouseMoveEventArgs e)
        {
            if (MouseMove != null)
            {
                MouseMove(this, e);
                return true;
            }

            return false;
        }
        
        protected virtual bool OnBlurred(FocusChangedEventArgs e)
        {
            if (Blurred != null)
            {
                Blurred(this, e);
                return true;
            }

            return false;
        }
        
        protected virtual bool OnFocused(FocusChangedEventArgs e)
        {
            if (Focused != null)
            {
                Focused(this, e);
                return true;
            }

            return false;
        }

        protected virtual bool OnKeyChar(KeyCharEventArgs e)
        {
            if (KeyChar != null)
            {
                KeyChar(this, e);
                return true;
            }

            return false;
        }

        protected virtual bool OnMouseScroll(MouseScrollEventArgs e)
        {
            if (MouseScroll != null)
            {
                MouseScroll(this, e);
                return true;
            }

            return false;
        }

        protected virtual bool OnKeyDown(KeyEventArgs e)
        {
            if (KeyDown != null)
            {
                KeyDown(this, e);
                return true;
            }

            return false;
        }
        
        protected virtual bool OnKeyUp(KeyEventArgs e)
        {
            if (KeyUp != null)
            {
                KeyUp(this, e);
                return true;
            }

            return false;
        }

        internal bool FireMouseEnter(MouseMoveEventArgs e)
        {
            return OnMouseEnter(e);
        }

        internal bool FireMouseLeave(MouseMoveEventArgs e)
        {
            return OnMouseLeave(e);
        }
        
        internal bool FireMouseDown(MouseButtonEventArgs e)
        {
            return OnMouseDown(e);
        }

        internal bool FireMouseUp(MouseButtonEventArgs e)
        {
            return OnMouseUp(e);
        }
        
        internal bool FireMouseMove(MouseMoveEventArgs e)
        {
            return OnMouseMove(e);
        }
        
        internal bool FireBlurred(FocusChangedEventArgs e)
        {
            return OnBlurred(e);
        }

        internal bool FireFocused(FocusChangedEventArgs e)
        {
            return OnFocused(e);
        }

        internal bool FireKeyChar(KeyCharEventArgs e)
        {
            return OnKeyChar(e);
        }

        internal bool FireMouseScroll(MouseScrollEventArgs e)
        {
            return OnMouseScroll(e);
        }

        internal bool FireKeyDown(KeyEventArgs e)
        {
            return OnKeyDown(e);
        }
        
        internal bool FireKeyUp(KeyEventArgs e)
        {
            return OnKeyUp(e);
        }
        
        protected void InvalidateRenderData()
        {
            if (!_isRenderDataDirty)
            {
                foreach (var child in Children.Collapse())
                    child._isRenderDataDirty = true;
                
                _isRenderDataDirty = true;
                var p = Parent;
                while (p != null)
                {
                    p._isRenderDataDirty = true;
                    p = p.Parent;
                }
            }
        }

        public void RecomputeRenderData()
        {
            if (_isRenderDataDirty)
            {
                // This is simple. All we need is our current opacity and tint (enabled = white, disabled = gray)
                var opacity = this.Opacity;
                var tint = Enabled ? Color.White : Color.Gray;
                
                // And our parent.
                var parent = this.Parent;
                
                // Recurse through the ancestry tree.
                while (parent != null)
                {
                    // Multiply current opacity by parent opacity.
                    opacity *= parent.Opacity;
                    
                    // Get the parent tint.
                    var pColor = parent.Enabled ? Color.White : Color.Gray;
                    
                    // Compute linear color values for the parent color.
                    var r = pColor.R;
                    var g = pColor.G;
                    var b = pColor.B;
                    
                    // And compute the values for our new color.
                    var br = tint.R * r;
                    var bg = tint.G * g;
                    var bb = tint.B * b;

                    // Apply the new computed values.
                    tint = new Color(br, bg, bb);

                    // Recurse up to the next parent.
                    parent = parent.Parent;
                }
                
                // Cache these computed values.
                _computedOpacity = opacity;
                _computedTint = tint;
                
                _isRenderDataDirty = false;
            }
        }
        
        public void NotifyPropertyModified(string name)
        {
            // Stub.
        }
        
        public class ElementCollection : ICollection<Element>
        {
            private Element _owner;
            private List<Element> _children = new List<Element>();

            public ElementCollection(Element owner)
            {
                _owner = owner;
            }
            
            public IEnumerator<Element> GetEnumerator()
            {
                return _children.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerable<Element> Collapse()
            {
                foreach (var child in _children)
                {
                    yield return child;
                    foreach (var subchild in child.Children.Collapse())
                    {
                        yield return subchild;
                    }
                }
            }
            
            public void Add(Element item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                if (item.Parent != null)
                    throw new InvalidOperationException("GUI element already has a parent.");

                item.Parent = _owner;
                
                // fixes a stupid fucking bug
                item._guiSystem = _owner.GuiSystem;
                foreach (var offspring in item.Children.Collapse())
                {
                    offspring._guiSystem = _owner.GuiSystem;
                }
                
                _children.Add(item);
            }

            public void Clear()
            {
                while (_children.Any())
                    Remove(_children.First());
            }

            public bool Contains(Element item)
            {
                return item != null && item.Parent == _owner;
            }

            public void CopyTo(Element[] array, int arrayIndex)
            {
                _children.CopyTo(array, arrayIndex);
            }

            public bool Remove(Element item)
            {
                if (item == null)
                    return false;

                if (item.Parent != _owner)
                    return false;
                
                item.Parent = null;
                item._guiSystem = null;
                return _children.Remove(item);
            }

            public int Count => _children.Count;
            public bool IsReadOnly => _owner.SupportsChildren;
        }
    }
}