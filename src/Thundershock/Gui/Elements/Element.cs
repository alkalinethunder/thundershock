using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
                item._guiSystem = _owner.GuiSystem;
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
        private string _name;
        private LayoutManager _layout;
        private Element _parent;
        private ElementCollection _children;
        private HorizontalAlignment _hAlign;
        private VerticalAlignment _vAlign;
        private Rectangle _bounds;

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
        
        public Element()
        {
            _children = new ElementCollection(this);
            _layout = new LayoutManager(this);
            _name = DefaultName;
        }

        protected internal void SetGuiSystem(GuiSystem gui)
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
        
        protected virtual Vector2 MeasureOverride()
        {
            var size = Vector2.Zero;

            foreach (var child in Children)
            {
                var childSize = child._layout.GetContentSize();
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
        
        protected virtual void OnPaint(GameTime gameTime, GuiRenderer renderer) {}
        protected virtual void OnUpdate(GameTime gameTime) {}
        protected virtual void OnPaint(GuiRenderer renderer) {}

        public class LayoutManager
        {
            private Element _owner;

            public Vector2 GetContentSize()
            {
                var measure = _owner.MeasureOverride();

                if (_owner._fixedWidth > 0)
                    measure.X = _owner._fixedWidth;

                if (_owner._fixedHeight > 0)
                    measure.Y = _owner._fixedHeight;
                
                if (_owner._minWidth > 0)
                    measure.X = MathF.Max(_owner._minWidth, measure.X);

                if (_owner._minHeight > 0)
                    measure.Y = MathF.Max(_owner._minHeight, measure.Y);

                if (_owner._maxWidth > 0)
                    measure.X = MathF.Min(_owner._maxWidth, measure.X);

                if (_owner._maxHeight > 0)
                    measure.Y = MathF.Min(_owner._maxHeight, measure.Y);




                return measure;
            }
            
            public LayoutManager(Element element)
            {
                _owner = element;
            }

            public void SetBounds(Rectangle rectangle)
            {
                var contentSize = this.GetContentSize();

                var bounds = Rectangle.Empty;

                switch (_owner.HorizontalAlignment)
                {
                    case HorizontalAlignment.Center:
                        bounds.Width = (int) contentSize.X;
                        bounds.X = rectangle.Right - ((rectangle.Width - bounds.Width) / 2);
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
                        bounds.Y = rectangle.Bottom - ((rectangle.Height - bounds.Height) / 2);
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