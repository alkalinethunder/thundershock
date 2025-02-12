﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Thundershock.Flumberboozles;
using Thundershock.Gui.Styling;
using Thundershock.Core.Input;
using Thundershock.Core;
using Thundershock.Core.Scripting;

namespace Thundershock.Gui.Elements
{
    /// <summary>
    /// Provides the base functionality for all GUI elements.
    /// </summary>
    [ScriptType]
    public abstract class Element : IPropertySetOwner
    {
        private bool _layoutInvalid = true;
        private GuiSystem _guiSystem;
        private float _opacity = 1;
        private float _minWidth;
        private float _minHeight;
        private float _maxWidth;
        private float _maxHeight;
        private float _fixedWidth;
        private float _fixedHeight;
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
        private Vector2 _viewportAlignment;
        private Vector2 _viewportPos;
        private FreePanel.CanvasAnchor _viewportAnchor = FreePanel.CanvasAnchor.Fill;

        public FreePanel.CanvasAnchor ViewportAnchor
        {
            get => _viewportAnchor;
            set
            {
                if (_viewportAnchor != value)
                {
                    _viewportAnchor = value;
                    InvalidateLayout();
                }
            }
        }
        
        public Vector2 ViewportAlignment
        {
            get => _viewportAlignment;
            set
            {
                if (_viewportAlignment != value)
                {
                    _viewportAlignment = value;
                    InvalidateLayout();
                }
            }
        }

        public Vector2 ViewportPosition
        {
            get => _viewportPos;
            set
            {
                if (_viewportPos != value)
                {
                    _viewportPos = value;
                    InvalidateLayout();
                }
            }
        }
        
        /// <summary>
        /// Gets a rectangle representing the area on the screen where this element is allowed to render.
        /// </summary>
        public Rectangle ClipBounds => _clipRect;

        /// <summary>
        /// Gets or sets a value indicating whether this UI element requires scissor clipping during its paint operation.
        /// </summary>
        public bool Clip { get; protected set; } = false;

        
        /// <summary>
        /// Gets or sets a value indicating whether this UI element can receive keyboard focus.
        /// </summary>
        public bool CanFocus { get; set; }
        
        /// <summary>
        /// Gets a rectangle representing the area where this UI element's children or content can be placed. 
        /// </summary>
        public Rectangle ContentRectangle => _contentRect;
        
        /// <summary>
        /// Gets or sets the background color of the UI element.
        /// </summary>
        public StyleColor BackColor { get; set; }  = StyleColor.Default;

        /// <summary>
        /// Gets or sets a value indicating whether this UI element receives mouse events or not.
        /// </summary>
        public bool IsInteractable { get; set; }
        
        /// <summary>
        /// Gets or sets the text that appears when the mouse hovers
        /// over this UI element or any of its children.
        /// </summary>
        public string ToolTip { get; set; }

        /// <summary>
        /// Gets or sets the font used by this GUI element when
        /// drawing text.
        /// </summary>
        public StyleFont Font
        {
            get => _font;
            set
            {
                if (_font != value)
                {
                    _font = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Gets an instance of this element's PropertySet, for storing attached properties for use
        /// by other elements.
        /// </summary>
        public PropertySet Properties => _props;
        
        /// <summary>
        /// Gets an instance of the GUI system this element belongs to.
        /// </summary>
        public GuiSystem GuiSystem
            => _guiSystem;
        
        /// <summary>
        /// Gets a rectangle representing the bounding box of this UI element.
        /// </summary>
        public Rectangle BoundingBox
            => _bounds;

        private string DefaultName
            => $"{GetType().Name}_{GetHashCode()}";

        public Vector2 GetOnScreenLocation()
        {
            var anchor = this.ViewportAnchor;
            var alignment = this.ViewportAlignment;
            var pos = this.ViewportPosition;
            var screen = GuiSystem.BoundingBox;
            var size = MyLayout.GetContentSize();

            var anchorLocation = screen.Size * new Vector2(anchor.Left, anchor.Top);
            var aSize = screen.Size * new Vector2(anchor.Right, anchor.Bottom);

            if (aSize.X * aSize.Y <= 0)
            {
                if (aSize.X <= 0)
                    aSize.X = size.X;

                if (aSize.Y <= 0)
                    aSize.Y = size.Y;
            }

            var aPos = aSize * alignment;

            anchorLocation -= aPos;

            return pos + anchorLocation;
        }
        
        public Element TopLevel
        {
            get
            {
                var p = Parent;

                if (p is RootElement)
                    return this;
                
                while (p != null)
                {
                    if (p.Parent is RootElement)
                        return p;
                    
                    p = p.Parent;
                }

                return p;
            }
        }
        
        /// <summary>
        /// Gets or sets the name of this element as shown in the editor and debugging tools.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value ?? DefaultName;
        }
        
        /// <summary>
        /// Gets or sets the horizontal content alignment of this element.
        /// </summary>
        public HorizontalAlignment HorizontalAlignment
        {
            get => _hAlign;
            set
            {
                if (_hAlign != value)
                {
                    _hAlign = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Gets or sets the vertical content alignment of this element.
        /// </summary>
        public VerticalAlignment VerticalAlignment
        {
            get => _vAlign;
            set
            {
                if (_vAlign != value)
                {
                    _vAlign = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value representing the opacity of this UI element.
        /// </summary>
        public float Opacity
        {
            get => _opacity;
            set => _opacity = MathHelper.Clamp(value, 0, 1);
        }

        /// <summary>
        /// Gets a value representing whether this element can paint on the screen.
        /// </summary>
        public abstract bool CanPaint { get; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this UI element is enabled or disabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value representing the foreground color of this element.
        /// </summary>
        public StyleColor ForeColor { get; set; } = StyleColor.Default;
        
        /// <summary>
        /// Gets the parent element that owns this element.
        /// </summary>
        public Element Parent
        {
            get => _parent;
            set => _parent = value;
        }

        /// <summary>
        /// Gets a value representing whether this element supports having child UI elements.
        /// </summary>
        protected virtual bool SupportsChildren => false;

        /// <summary>
        /// Gets a modifiable and enumerable list of this element's children.
        /// </summary>
        public ElementCollection Children => _children;

        /// <summary>
        /// Gets a value indicating whether this element currently has keyboard focus.
        /// </summary>
        public bool IsFocused => GuiSystem.FocusedElement == this;
        
        /// <summary>
        /// Gets a value indicating whether this or any child element currently has keyboard focus.
        /// </summary>
        public bool HasAnyFocus => IsFocused || _children.Any(x => x.HasAnyFocus);

        /// <summary>
        /// Gets or sets a value indicating the space between the parent bounds and this element's bounding box.
        /// </summary>
        public Padding Padding
        {
            get => _padding;
            set
            {
                if (_padding != value)
                {
                    _padding = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the space between this element's bounding box and the content rectangle.
        /// </summary>
        public Padding Margin
        {
            get => _margin;
            set
            {
                if (_margin != value)
                {
                    _margin = value;
                    InvalidateLayout();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the fixed width of this UI element. Default is 0 meaning that width is determined based on content size.
        /// </summary>
        public float FixedWidth
        {
            get => _fixedWidth;
            set
            {
                if (MathF.Abs(_fixedWidth - value) >= 0.0001f)
                {
                    _fixedWidth = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the fixed height of this UI element. Default is 0 meaning that height is determined by content size.
        /// </summary>
        public float FixedHeight
        {
            get => _fixedHeight;
            set
            {
                if (MathF.Abs(_fixedHeight - value) >= 0.0001f)
                {
                    _fixedHeight = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum content width.
        /// </summary>
        public float MinimumWidth
        {
            get => _minWidth;
            set
            {
                if (MathF.Abs(_minWidth - value) >= 0.0001f)
                {
                    _minWidth = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum content height.
        /// </summary>
        public float MinimumHeight
        {
            get => _minHeight;
            set
            {
                if (MathF.Abs(_minHeight - value) >= 0.0001f)
                {
                    _minHeight = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Gets or sets the visibility of this UI element.
        /// </summary>
        public Visibility Visibility
        {
            get => _visibility;
            set
            {
                if (_visibility != value)
                {
                    _visibility = value;
                    InvalidateLayout();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the maximum content width.
        /// </summary>
        public float MaximumWidth
        {
            get => _maxWidth;
            set
            {
                if (MathF.Abs(_maxWidth - value) >= 0.0001f)
                {
                    _maxWidth = value;
                    InvalidateLayout();
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum content height.
        /// </summary>
        public float MaximumHeight
        {
            get => _maxHeight;
            set
            {
                if (MathF.Abs(_maxHeight - value) >= 0.0001f)
                {
                    _maxHeight = value;
                    InvalidateLayout();
                }
            }
        }
        
        /// <summary>
        /// Gets a value representing the actual size of the element's content.
        /// </summary>
        public Vector2 ActualSize { get; private set; }
        
        /// <summary>
        /// Creates a new instance of the <se cref="Element" /> class.
        /// </summary>
        public Element()
        {
            _children = new ElementCollection(this);
            _layout = new LayoutManager(this);
            _name = DefaultName;

            _props = new(this);

            if (this is RootElement)
                _layoutInvalid = false;
        }

        /// <summary>
        /// Removes this element from its parent element.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this element was added as a top-level UI element to the UI system, then this method removes
        /// the element from the list of toplevels. Otherwise this method removes the element from it's parent's
        /// child list.
        /// </pare>
        /// </remarks>
        public void RemoveFromParent()
        {
            if (Parent is RootElement re)
            {
                re.GuiSystem.RemoveFromViewport(this);
            }
            else if (Parent != null)
            {
                Parent.Children.Remove(this);
            }
        }

        protected internal void SetGuiSystem(GuiSystem gui)
        {
            if (_guiSystem != null)
                throw new InvalidOperationException("You can only set the GuiSystem of the Root Element.");

            _guiSystem = gui;
        }
        
        /// <summary>
        /// Called when the element's content rectangle has been calculated. Use this method
        /// to place child elements and content within the content rectangle.
        /// </summary>
        /// <param name="contentRectangle">The calculated content rectangle of the element.</param>
        protected virtual void ArrangeOverride(Rectangle contentRectangle)
        {
            foreach (var child in Children)
            {
                child.MyLayout.SetBounds(contentRectangle);
            }
        }

        private bool NeedsLayoutUpdate => _layoutInvalid || (Parent != null && Parent.NeedsLayoutUpdate);
        
        /// <summary>
        /// Called during layout to calculate the size of the element's children or content.
        /// </summary>
        /// <param name="alottedSize">A suggested size that all content should fit within. Zero means that there is no limit.</param>
        /// <returns>The calculated size of the element.</returns>
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

        /// <summary>
        /// Gets an instance of this element's Layout Manager.
        /// </summary>
        protected LayoutManager MyLayout => _layout;
        
        /// <summary>
        /// Paints the element on the screen if painting is enabled.
        /// </summary>
        /// <param name="gameTime">Time since last tick.</param>
        /// <param name="renderer">An instance of the GUI renderer for this element.</param>
        public void Paint(GameTime gameTime, GuiRenderer renderer)
        {
            OnPaint(gameTime, renderer);
        }

        /// <summary>
        /// Calculates the size of this element's content.
        /// </summary>
        /// <param name="alottedSize">The desired maximum size of the element.</param>
        /// <returns>The calculated element size.</returns>
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
            
            var measure = MeasureOverride(alottedSize);

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
            
            measure += Margin.Size + Padding.Size;
            
            ActualSize = measure;
            
            return measure;
        }
        
        
        /// <summary>
        /// Called when it's time for the GUI element to paint.
        /// </summary>
        /// <param name="gameTime">Time since the last tick.</param>
        /// <param name="renderer">An instance of this GUI element's renderer.</param>
        protected virtual void OnPaint(GameTime gameTime, GuiRenderer renderer) {}

        /// <summary>
        /// Provides functionality for performing layout operations on GUI elements.
        /// </summary>
        public class LayoutManager
        {
            private Element _owner;
            
            public Vector2 GetContentSize(Vector2 alottedSize = default)
            {
                return _owner.Measure(alottedSize);
            }

            public Vector2 GetChildContentSize(Element element)
            {
                if (element.NeedsLayoutUpdate)
                    return element.MyLayout.GetContentSize();
                else
                    return element.ActualSize;
            }
            
            internal LayoutManager(Element element)
            {
                _owner = element;
            }

            public void SetChildBounds(Element elem, Rectangle rect)
            {
                elem.MyLayout.SetBounds(rect);
            }

            public void SetBounds(Rectangle rectangle)
            {
                if (_owner.NeedsLayoutUpdate)
                {
                    var contentSize = GetContentSize(rectangle.Size);

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
                        case VerticalAlignment.Top:
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

                    _owner.ArrangeOverride(_owner.ContentRectangle);
                    _owner._layoutInvalid = false;
                }
            }

            private Rectangle ComputeClipRect()
            {
                var e = _owner;
                var rect = e.BoundingBox;
                while (e != null)
                {
                    if (e is RootElement) break;
                    
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
        
        public void NotifyPropertyModified(string name)
        {
            InvalidateLayout();
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

            public Element this[int index]
            {
                get => _children[index];
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
                                
                _children.Add(item);
                item.InvalidateLayout();
                
                foreach (var offspring in item.Children.Collapse())
                {
                    offspring._guiSystem = _owner.GuiSystem;
                    offspring.InvalidateLayout();
                }

            }

            public void Insert(int index, Element element)
            {
                if (element == null)
                    throw new ArgumentNullException(nameof(element));

                if (index < 0 || index > Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                
                if (element.Parent != null)
                    throw new InvalidOperationException("GUI element already has a parent.");

                element.Parent = _owner;

                element._guiSystem = _owner._guiSystem;
                

                _children.Insert(index, element);
                element.InvalidateLayout();
                
                foreach (var offspring in element.Children.Collapse())
                {
                    offspring._guiSystem = _owner.GuiSystem;
                    offspring.InvalidateLayout();
                }
            }
            
            public void Clear()
            {
                _owner.InvalidateLayout();
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

                _owner.InvalidateLayout();
                
                return _children.Remove(item);
            }

            public int Count => _children.Count;
            public bool IsReadOnly => _owner.SupportsChildren;
        }

        /// <summary>
        /// Invalidates the current layout of this element and its children.
        /// This discards the clipping rectangle and causes the bounding rectangles
        /// to be recomputed on the next engine tick. Calling this method religiously
        /// will severely impact the framerate.
        /// </summary>
        public void InvalidateLayout()
        {
            if (this is RootElement) return;
            
            if (!_layoutInvalid)
            {
                foreach (var offspring in Children.Collapse())
                    offspring._layoutInvalid = true;
                
                _layoutInvalid = true;

                var p = Parent;
                while (p != null && !(p is RootElement))
                {
                    p._layoutInvalid = true;
                    p = p.Parent;
                }

            }
        }
    }
}