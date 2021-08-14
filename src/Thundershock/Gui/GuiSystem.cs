using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Gui.Elements;
using Thundershock.Gui.Styling;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;

namespace Thundershock.Gui
{
    public sealed class GuiSystem
    {
        private static Type _defaultStyleType;

        private RootElement _rootElement;
        private Element.ElementCollection _topLevels;
        private GuiRendererState _guiRendererState;
        private GuiRenderer _guiRenderer;
        private float _viewWidth = 1600;
        private float _viewHeight = 900;
        private GraphicsProcessor _gpu;
        private GuiStyle _activeStyle;
        private bool _debugShowBounds;
        private Font _debugFont;
        private Element _focused;
        private Element _hovered;
        private Element _down;
        private string _tooltip;
        private Vector2 _tooltipPosition;
        private Renderer2D _renderer;

        private bool _showFPS = false;
        
        public event EventHandler<MouseMoveEventArgs> GlobalMouseMove;

        public Font FallbackFont => _debugFont;
        
        public Element FocusedElement => _focused;

        public GuiStyle Style => _activeStyle;

        public Rectangle BoundingBox => new Rectangle(0, 0, _viewWidth, _viewHeight);

        public GraphicsProcessor Graphics => _gpu;
        
        public GuiSystem(GraphicsProcessor gpu)
        {
            _rootElement = new(this);
            _topLevels = new(_rootElement);
            _gpu = gpu;
            _renderer = new Renderer2D(_gpu);
            _guiRendererState = new GuiRendererState(this);
            _guiRenderer = new GuiRenderer(_renderer, _guiRendererState);
            
            _debugFont = Font.GetDefaultFont(_gpu);

            if (_defaultStyleType != null)
            {
                LoadStyle(_defaultStyleType);
            }
            else
            {
                LoadStyle<BasicStyle>();
            }

        }
        
        private void LoadStyle(Type styleType)
        {
            var style = (GuiStyle) Activator.CreateInstance(styleType, null);

            if (_activeStyle != null)
                _activeStyle.Unload();

            _activeStyle = style;

            _activeStyle.Load(this);
        }

        public void SetViewportSize(float width, float height)
        {
            var shouldPerformLayout = false;
            if (MathF.Abs(_viewWidth - width) >= 0.1f)
            {
                _viewWidth = width;
                shouldPerformLayout = true;
            }

            if (MathF.Abs(_viewHeight - height) > 0.1f)
            {
                _viewHeight = height;
                shouldPerformLayout = true;
            }

            if (shouldPerformLayout)
                PerformLayout();
        }
        
        public void LoadStyle<T>() where T : GuiStyle, new()
        {
            if (_activeStyle != null)
                _activeStyle.Unload();

            _activeStyle = new T();

            _activeStyle.Load(this);
        }
        
        public bool KeyUp(KeyEventArgs e)
        {
            return Bubble(_focused, x => x.FireKeyUp(e));
        }

        public bool KeyDown(KeyEventArgs e)
        {
            if (e.Key == Keys.F3)
            {
                _showFPS = !_showFPS;
            }
            
            return Bubble(_focused, x => x.FireKeyDown(e));
        }

        public bool MouseScroll(MouseScrollEventArgs e)
        {
            var pos = ScreenToViewport(new Vector2(e.X, e.Y));
            var hovered = FindElement((int) pos.X, (int) pos.Y, false);
            return Bubble(hovered, x => x.FireMouseScroll(e));
        }

        public bool KeyChar(KeyCharEventArgs e)
        {
            return Bubble(_focused, x => x.FireKeyChar(e));
        }

        private bool Bubble(Element element, Func<Element, bool> predicate)
        {
            var e = element;
            while (e != null)
            {
                if (e.IsInteractable && predicate(e))
                    return true;
                e = e.Parent;
            }

            return false;
        }

        
        public void SetFocus(Element element)
        {
            if (_focused != element)
            {
                var evt = new FocusChangedEventArgs(_focused, element);

                if (_focused != null)
                {
                    _focused.FireBlurred(evt);
                }

                _focused = element;
                
                if (element != null)
                {
                    element.FireFocused(evt);
                    _focused = element;
                }
            }
        }
        
        public bool MouseUp(MouseButtonEventArgs e)
        {
            var pos = ScreenToViewport(new Vector2(e.X, e.Y));
            var hovered = FindElement((int) pos.X, (int) pos.Y);
            
            var result = Bubble(_down, x => x.FireMouseUp(e));

            if (_down == hovered)
            {
                if (hovered == null || hovered.CanFocus)
                    SetFocus(hovered);
                _down = null;
            }

            return result;
        }

        public bool MouseDown(MouseButtonEventArgs e)
        {
            var pos = ScreenToViewport(new Vector2(e.X, e.Y));
            var hovered = FindElement((int) pos.X, (int) pos.Y);
            
            _down = hovered;
            return Bubble(_down, x => x.FireMouseDown(e));
        }

        public bool MouseMove(MouseMoveEventArgs e)
        {
            GlobalMouseMove?.Invoke(this, e);

            var pos = ScreenToViewport(new Vector2(e.X, e.Y));
            var hovered = FindElement((int) pos.X, (int) pos.Y);

            // MouseEnter and MouseLeave.
            if (_hovered != hovered)
            {
                if (_hovered != null && (hovered == null || !hovered.HasParent(_hovered)))
                {
                    _hovered.FireMouseLeave(e);
                }

                _hovered = hovered;

                if (_hovered != null)
                    _hovered.FireMouseEnter(e);
            }
            
            _tooltip = null;

            // Tool-tips.
            if (hovered != null)
            {
                var tooltip = GetToolTip(hovered);
                if (!string.IsNullOrWhiteSpace(tooltip))
                {
                    _tooltip = tooltip;
                    _tooltipPosition = ScreenToViewport( new Vector2(e.X, e.Y));
                }
            }
            
            return Bubble(_hovered, x => x.FireMouseMove(e));
        }

        public void AddToViewport(Element element)
        {
            _topLevels.Add(element);
        }
        
        public void Update(GameTime gameTime)
        {
            PerformLayout();

            for (var i = 0; i < _topLevels.Count; i++)
            {
                var tl = _topLevels[i];
                tl.Update(gameTime);
            }
        }
        
        private void PerformLayout()
        {
            var screenRectangle = new Rectangle(0, 0, _viewWidth, _viewHeight);

            var layout = _rootElement.RootLayoutManager;
            
            foreach (var element in _topLevels)
            {
                var anchor = element.ViewportAnchor;
                var align = element.ViewportAlignment;
                var aPos = element.ViewportPosition;

                var rect = screenRectangle;
                
                rect.Width *= anchor.Right;
                rect.Height *= anchor.Bottom;

                if (rect.IsEmpty)
                {
                    // special case.
                    var size = layout.GetChildContentSize(element);

                    if (rect.Width <= 0)
                        rect.Width = size.X;
                    
                    if (rect.Height <= 0)
                        rect.Height = size.Y;
                }

                var pos = screenRectangle.Size * new Vector2(anchor.Left, anchor.Top);
                pos -= (rect.Size * align);
                pos += aPos;

                rect.X = pos.X;
                rect.Y = pos.Y;
                
                layout.SetChildBounds(element, rect);
            }
        }
        
        public void Render(GameTime gameTime)
        {
            var screen = BoundingBox;
            var projection = Matrix4x4.CreateOrthographicOffCenter(0, screen.Width, screen.Height, 0, _renderer.MaxBatchCount, -_renderer.MaxBatchCount);

            _renderer.ProjectionMatrix = projection;

            _guiRendererState.Clip = _gpu.ViewportBounds;
            
            _renderer.Begin();

            foreach (var elem in _topLevels)
            {
                PaintElements(gameTime, elem);
            }

            if (!string.IsNullOrWhiteSpace(_tooltip))
            {
                var font = _activeStyle.DefaultFont;

                var wrapped = TextBlock.WordWrap(font, _tooltip, 450);

                var measure = font.MeasureString(wrapped) + new Vector2(10, 10);

                var bottomLeft = _tooltipPosition + measure;
                if (bottomLeft.X >= BoundingBox.Right)
                {
                    _tooltipPosition.X -= (bottomLeft.X - BoundingBox.Right);
                }

                if (bottomLeft.Y >= BoundingBox.Bottom)
                {
                    _tooltipPosition.Y -= bottomLeft.Y - BoundingBox.Bottom;
                }
                
                _renderer.FillRectangle(new Rectangle((int)_tooltipPosition.X, (int)_tooltipPosition.Y, (int)measure.X, (int)measure.Y), Color.Black);

                _renderer.DrawString(font, wrapped, _tooltipPosition + new Vector2(5, 5), Color.White);
            }

            if (_showFPS)
            {
                DrawFPS(gameTime);
            }
            
            _renderer.End();
        }

        private void DrawFPS(GameTime gameTime)
        {
            var text = Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds).ToString();

            var m = _debugFont.MeasureString(text);
            var rect = new Rectangle(0, 0, m.X, m.Y);

            _renderer.FillRectangle(rect, Color.Black);
            _renderer.DrawString(_debugFont, text, Vector2.Zero, Color.White);
        }

        private Element FindElement(int x, int y, bool requireInteractible = true)
        {
            var screen = BoundingBox;
            
            for (var i = _topLevels.Count - 1; i >= 0; i--)
            {
                var elem = _topLevels[i];
                
                var f = FindElement(elem, x, y, requireInteractible);

                if (f != null)
                    return f;
            }

            return null;
        }

        private Element FindElement(Element elem, int x, int y, bool requireInteractible = true)
        {
            // check the visibility of the element. If it's not visible, return.
            if (elem.Visibility != Visibility.Visible)
                return null;
            
            // If the element is disabled, return.
            if (!elem.Enabled)
                return null;
            
            // Check the bounds of the element. If the cursor's not inside them, return.
            var b = elem.BoundingBox;
            if (!(x >= b.Left && x <= b.Right && y >= b.Top && y <= b.Bottom))
                return null;
            
            foreach (var child in elem.Children.ToArray().Reverse())
            {
                var f = FindElement(child, x, y);
                if (f != null)
                    return f;
            }
            
            if (elem.IsInteractable || !requireInteractible)
                return elem;

            return null;
        }

        private string GetToolTip(Element element)
        {
            var e = element;
            while (e != null)
            {
                if (!string.IsNullOrWhiteSpace(e.ToolTip))
                {
                    return e.ToolTip;
                }

                e = e.Parent;
            }

            return null;
        }

        public static void SetDefaultStyle<T>() where T : GuiStyle, new()
        {
            _defaultStyleType = typeof(T);
        }

        public Vector2 ScreenToViewport(Vector2 pos)
        {
            pos.X /= _gpu.ViewportBounds.Width;
            pos.Y /= _gpu.ViewportBounds.Height;
            pos.X *= _viewWidth;
            pos.Y *= _viewHeight;

            return pos;
        }

        public Vector2 ViewportToScreen(Vector2 pos)
        {
            pos.X /= _viewWidth;
            pos.Y /= _viewHeight;
            pos.X *= _gpu.ViewportBounds.Width;
            pos.Y *= _gpu.ViewportBounds.Height;

            return pos;
        }
        
        public Vector2 ScreenToViewport(float x, float y)
            => ScreenToViewport(new Vector2(x, y));

        public Vector2 ViewportToScreen(float x, float y)
            => ViewportToScreen(new Vector2(x, y));

        private void PaintElements(GameTime gameTime, Element element)
        {
            // Don't paint invisible elements.
            if (element.Visibility != Visibility.Visible)
                return;
            
            // Don't paint fully transparent elements.
            if (element.Opacity <= 0)
                return;
            
            // Don't paint 0-size elements.
            if (element.BoundingBox.IsEmpty)
                return;
            
            // Clipping rectangle determines whether the element is actually visible on-screen.
            // However, we do not actually do GPU clipping unless the element requests it.
            // This is an optimization - because changing the scissor test region requires
            // ending the current render batch.
            var clip = element.ClipBounds;
            
            // Clip the element if the clip rect is empty...
            if (clip.IsEmpty)
                return;
            
            // Update the GPU's scissor test bounds IF THIS ELEMENT WANTS US TO.
            if (element.Clip)
            {
                _renderer.SetClipBounds(clip);
            }
            
            if (element.CanPaint)
            {
                // Paint, damn you.
                element.Paint(gameTime, _guiRenderer);
                
                // Disable scissoring.
                // _gpu.EnableScissoring = false;
                // _renderer.SetClipBounds(null);
            }

            // Recurse through the element's children.
            foreach (var child in element.Children)
            {
                // Paint the child.
                PaintElements(gameTime, child);
            }
            
            // We have painted the element and its children. If clipping was requested,
            // this is where we turn it off.
            if (element.Clip)
            {
                _renderer.SetClipBounds(null);
            }
        }

        public void RemoveFromViewport(Element element)
        {
            _topLevels.Remove(element);
        }

        internal class GuiRendererState
        {
            internal GuiRendererState(GuiSystem owner)
            {
                if (owner._guiRendererState != null)
                    throw new InvalidOperationException("Gui renderer state already bound.");
            }

            public float Opacity { get; set; } = 1;
            public Color Tint { get; set; } = Color.White;
            public Rectangle Clip { get; set; }
        }
    }
}