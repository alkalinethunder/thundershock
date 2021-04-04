using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Flumberboozles;
using Thundershock.Input;

namespace Thundershock.Gui.Elements
{
    public abstract class Element
    {
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

        private GuiSystem _guiSystem;
        private int _minWidth;
        private int _minHeight;
        private int _maxWidth;
        private int _maxHeight;
        private int _fixedWidth;
        private int _fixedHeight;
        private int _widthUnitRounding;
        private int _heightUnitRounding;
        private string _name;
        private LayoutManager _layout;
        private Element _parent;
        private ElementCollection _children;
        private HorizontalAlignment _hAlign;
        private VerticalAlignment _vAlign;
        private Rectangle _bounds;
        private Rectangle _contentRect;

        public Rectangle ContentRectangle => _contentRect;
        
        public PropertySet Properties { get; } = new PropertySet();

        public int WidthUnitRounding
        {
            get => _widthUnitRounding;
            set => _widthUnitRounding = value;
        }

        public int HeightUnitRounding
        {
            get => _heightUnitRounding;
            set => _heightUnitRounding = value;
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
            set => _hAlign = value;
        }

        public VerticalAlignment VerticalAlignment
        {
            get => _vAlign;
            set => _vAlign = value;
        }
        
        
        
        public float Opacity { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        
        public Element Parent
        {
            get => _parent;
            set => _parent = value;
        }

        protected virtual bool SupportsChildren => false;

        public ElementCollection Children => _children;

        public bool IsFocused => GuiSystem.FocusedElement == this;
        public bool HasAnyFocus => IsFocused || _children.Any(x => x.HasAnyFocus);

        public Padding Padding { get; set; }
        public Padding Margin { get; set; }
        
        public int FixedWidth
        {
            get => _fixedWidth;
            set => _fixedWidth = value;
        }

        public int FixedHeight
        {
            get => _fixedHeight;
            set => _fixedHeight = value;
        }

        public int MinimumWidth
        {
            get => _minWidth;
            set => _minWidth = value;
        }

        public int MinimumHeight
        {
            get => _minHeight;
            set => _minHeight = value;
        }

        public int MaximumWidth
        {
            get => _maxWidth;
            set => _maxWidth = value;
        }

        public int MaximumHeight
        {
            get => _maxHeight;
            set => _maxHeight = value;
        }
        
        public Vector2 ActualSize { get; private set; }
        
        public Element()
        {
            _children = new ElementCollection(this);
            _layout = new LayoutManager(this);
            _name = DefaultName;
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
                var contentSize = this.GetContentSize(rectangle.Size.ToVector2());

                // Apply padding.
                rectangle.X += _owner.Padding.Left;
                rectangle.Y += _owner.Padding.Top;
                rectangle.Width -= _owner.Padding.Width;
                rectangle.Height -= _owner.Padding.Height;
                
                var bounds = Rectangle.Empty;

                switch (_owner.HorizontalAlignment)
                {
                    case HorizontalAlignment.Center:
                        bounds.Width = (int) contentSize.X;
                        bounds.X = rectangle.Left + ((rectangle.Width - bounds.Width) / 2);
                        break;
                    case HorizontalAlignment.Left:
                        bounds.Width = (int) contentSize.X;
                        bounds.X = rectangle.Left;
                        break;
                    case HorizontalAlignment.Right:
                        bounds.Width = (int) contentSize.X;
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
                        bounds.Height = (int) contentSize.Y;
                        bounds.Y = rectangle.Top + ((rectangle.Height - bounds.Height) / 2);
                        break;
                    case Gui.VerticalAlignment.Top:
                        bounds.Height = (int) contentSize.Y;
                        bounds.Y = rectangle.Top;
                        break;
                    case VerticalAlignment.Bottom:
                        bounds.Height = (int) contentSize.Y;
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
                
                _owner.ArrangeOverride(bounds);
            }
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
        
    }
}