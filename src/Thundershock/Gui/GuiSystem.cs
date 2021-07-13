using System;
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

        private float _viewWidth = 1600;
        private float _viewHeight = 900;
        private GraphicsProcessor _gpu;
        private GuiStyle _activeStyle;
        private RootElement _rootElement;
        private bool _debugShowBounds;
        private Font _debugFont;
        private Element _focused;
        private Element _hovered;
        private Element _down;
        private string _tooltip;
        private Vector2 _tooltipPosition;
        private Renderer2D _renderer;
        
        public event EventHandler<MouseMoveEventArgs> GlobalMouseMove;

        public Font FallbackFont => _debugFont;
        
        public Element FocusedElement => _focused;

        public GuiStyle Style => _activeStyle;

        public Rectangle BoundingBox => _rootElement.BoundingBox;

        public GraphicsProcessor Graphics => _gpu;

        public bool ShowBoundingRects
        {
            get => _debugShowBounds;
            set => _debugShowBounds = value;
        }

        public GuiSystem(GraphicsProcessor gpu)
        {
            _gpu = gpu;
            
            _debugFont = Font.GetDefaultFont(_gpu);
            _rootElement = new RootElement(this);

            if (_defaultStyleType != null)
            {
                LoadStyle(_defaultStyleType);
            }
            else
            {
                LoadStyle<BasicStyle>();
            }

            _renderer = new Renderer2D(_gpu);
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
            return Bubble(_focused, x => x.FireKeyDown(e));
        }

        public bool MouseScroll(MouseScrollEventArgs e)
        {
            var pos = ScreenToViewport(new Vector2(e.X, e.Y));
            var hovered = FindElement((int) pos.X, (int) pos.Y);
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
            _rootElement.Children.Add(element);
        }
        
        public void Update(GameTime gameTime)
        {
            PerformLayout();
            
            _rootElement.Update(gameTime);
        }
        
        private void PerformLayout()
        {
            var screenRectangle = new Rectangle(0, 0, _viewWidth, _viewHeight);

            var rootLayout = _rootElement.RootLayoutManager;

            rootLayout.SetBounds(screenRectangle);
        }
        
        public void Render(GameTime gameTime)
        {
            var projection = Matrix4x4.CreateOrthographicOffCenter(0, _viewWidth, _viewHeight, 0, -1, 1);

            _renderer.ProjectionMatrix = projection;

            PaintElements(gameTime, _rootElement);
            
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

                _renderer.Begin();

                _renderer.FillRectangle(new Rectangle((int)_tooltipPosition.X, (int)_tooltipPosition.Y, (int)measure.X, (int)measure.Y), Color.Black);

                _renderer.DrawString(font, wrapped, _tooltipPosition + new Vector2(5, 5), Color.White);

                _renderer.End();
            }
        }

        private Element FindElement(int x, int y)
        {
            return FindElement(_rootElement, x, y);
        }

        private Element FindElement(Element elem, int x, int y)
        {
            foreach (var child in elem.Children.ToArray().Reverse())
            {
                var f = FindElement(child, x, y);
                if (f != null)
                    return f;
            }

            var b = elem.BoundingBox;

            if (elem.IsInteractable && x >= b.Left && x <= b.Right && y >= b.Top && y <= b.Bottom)
            {
                return elem;
            }
            
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
            // Skip rendering if the element is explicitly invisible.
            if (element.Visibility != Visibility.Visible)
                return;
            
            // Re-compute render data for the element if it is dirty.
            element.RecomputeRenderData();

            // Skip rendering if the element is fully transparent.
            if (element.ComputedOpacity <= 0)
                return;

            var clip = element.ClipBounds;
            
            // Skip rendering if the clipping rectangle for the element is empty
            // (the element is either off-screen or outside the bounds of its parent.)
            if (clip.IsEmpty)
                return;

            if (element.CanPaint)
            {
                // Get the computed tint value.
                var tint = element.ComputedTint;

                // Set up the GUI renderer.
                var gRenderer = new GuiRenderer(_renderer, element.ComputedOpacity, tint);

                // Set up the scissor testing for the element.
                _gpu.EnableScissoring = true;
                _gpu.ScissorRectangle = clip;

                // Begin the render batch.
                _renderer.Begin();

                // Paint, damn you.
                element.Paint(gameTime, gRenderer);

                // Debug rects if they're enabled.
                if (_debugShowBounds)
                {
                    var debugRenderer = new GuiRenderer(_renderer, 1, Color.White);

                    debugRenderer.DrawRectangle(element.BoundingBox, Color.White, 1);

                    var text = $"{element.Name}{Environment.NewLine}BoundingBox={element.BoundingBox}";
                    var measure = _debugFont.MeasureString(text);
                    var pos = new Vector2((element.BoundingBox.Left + ((element.BoundingBox.Width - measure.X) / 2)),
                        element.BoundingBox.Top + ((element.BoundingBox.Height - measure.Y) / 2));

                    debugRenderer.DrawString(_debugFont, text, pos, Color.White, 2);
                }

                // End the batch.
                _renderer.End();

                // Disable scissoring.
                _gpu.EnableScissoring = false;
            }

            // Recurse through the element's children.
            foreach (var child in element.Children)
            {
                // Paint the child.
                PaintElements(gameTime, child);
            }
        }
    }
}