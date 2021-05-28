using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Gui.Elements;
using Thundershock.Gui.Styling;
using Thundershock.Input;
using Thundershock.Rendering;

namespace Thundershock.Gui
{
    public sealed class GuiSystem : SceneComponent
    {
        private GuiStyle _activeStyle;
        private RootElement _rootElement;
        private bool _debugShowBounds = false;
        private SpriteFont _debugFont;
        private Element _focused;
        private Element _hovered;
        private Element _down;
        private InputManager _input;
        private string _tooltip;
        private Vector2 _tooltipPosition;

        public SpriteFont FallbackFont => _debugFont;
        
        public Element FocusedElement => _focused;

        public GuiStyle Style => _activeStyle;

        public Rectangle BoundingBox => _rootElement.BoundingBox;
        
        public bool ShowBoundingRects
        {
            get => _debugShowBounds;
            set => _debugShowBounds = value;
        }
        
        protected override void OnLoad()
        {
            base.OnLoad();
            _input = App.GetComponent<InputManager>();
            _rootElement = new RootElement(this);

            _debugFont = App.EngineContent.Load<SpriteFont>("Fonts/DebugSmall");
            
            _input.MouseMove += HandleMouseMove;
            _input.MouseDown += HandleMouseDown;
            _input.MouseUp += HandleMouseUp;
            _input.KeyChar += HandleKeyChar;
            _input.MouseScroll += HandleMouseScroll;
            _input.KeyDown += HandleKeyDown;
            _input.KeyUp += HandleKeyUp;

            LoadStyle<BasicStyle>();
        }

        protected override void OnUnload()
        {
            _input.MouseMove -= HandleMouseMove;
            _input.MouseDown -= HandleMouseDown;
            _input.MouseUp -= HandleMouseUp;
            _input.KeyChar -= HandleKeyChar;
            _input.MouseScroll -= HandleMouseScroll;
            _input.KeyDown -= HandleKeyDown;
            _input.KeyUp -= HandleKeyUp;
        }

        public void LoadStyle<T>() where T : GuiStyle, new()
        {
            if (_activeStyle != null)
                _activeStyle.Unload();

            _activeStyle = new T();

            _activeStyle.Load(this);
        }
        
        private void HandleKeyUp(object sender, KeyEventArgs e)
        {
            Bubble(_focused, x => x.FireKeyUp(e));
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            Bubble(_focused, x => x.FireKeyDown(e));
        }

        private void HandleMouseScroll(object sender, MouseScrollEventArgs e)
        {
            Bubble(_hovered, x => x.FireMouseScroll(e));
        }

        private void HandleKeyChar(object sender, KeyCharEventArgs e)
        {
            Bubble(_focused, x => x.FireKeyChar(e));
        }

        private void Bubble(Element element, Func<Element, bool> predicate)
        {
            var e = element;
            while (e != null)
            {
                if (predicate(e))
                    break;
                e = e.Parent;
            }
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

                if (element != null)
                {
                    element.FireFocused(evt);
                    _focused = element;
                }
            }
        }
        
        private void HandleMouseUp(object sender, MouseButtonEventArgs e)
        {
            var pos = Scene.ScreenToViewport(new Vector2(e.XPosition, e.YPosition));
            var hovered = FindElement((int) pos.X, (int) pos.Y);

            Bubble(_down, x => x.FireMouseUp(e));
            
            if (_down == hovered)
            {
                if (hovered == null || hovered.CanFocus)
                    SetFocus(hovered);
                _down = null;
            }
        }

        private void HandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = Scene.ScreenToViewport(new Vector2(e.XPosition, e.YPosition));
            var hovered = FindElement((int) pos.X, (int) pos.Y);
            
            _down = hovered;
            Bubble(_down, x => x.FireMouseDown(e));
        }

        private void HandleMouseMove(object sender, MouseMoveEventArgs e)
        {
            var pos = Scene.ScreenToViewport(new Vector2(e.XPosition, e.YPosition));
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

            Bubble(_hovered, x => x.FireMouseMove(e));

            _tooltip = null;

            // Tool-tips.
            if (hovered != null)
            {
                var tooltip = GetToolTip(hovered);
                if (!string.IsNullOrWhiteSpace(tooltip))
                {
                    _tooltip = tooltip;
                    _tooltipPosition = Scene.ScreenToViewport( new Vector2(e.XPosition, e.YPosition));
                }
            }
        }

        public void AddToViewport(Element element)
        {
            _rootElement.Children.Add(element);
        }
        
        protected override void OnUpdate(GameTime gameTime)
        {
            base.OnUpdate(gameTime);
            
            PerformLayout();

            _rootElement.Update(gameTime);
        }

        private float ComputeElementOpacity(Element element)
        {
            var opacity = element.Opacity;
            var parent = element.Parent;
            while (parent != null)
            {
                opacity = opacity * parent.Opacity;
                parent = parent.Parent;
            }

            return opacity;
        }

        private void PerformLayout()
        {
            var screenRectangle = Scene.ViewportBounds;

            var rootLayout = _rootElement.RootLayoutManager;

            rootLayout.SetBounds(screenRectangle);
        }
        
        private Color ComputeElementTint(Element element)
        {
            var color = element.Enabled ? Color.White : Color.Gray;
            var parent = element.Parent;
            while (parent != null)
            {
                var pColor = parent.Enabled ? Color.White : Color.Gray;

                var r = (float) pColor.R / 255f;
                var g = (float) pColor.G / 255f;
                var b = (float) pColor.B / 255f;

                var br = (byte) (color.R * r);
                var bg = (byte) (color.G * g);
                var bb = (byte) (color.B * b);

                color = new Color(br, bg, bb);
                
                parent = parent.Parent;
            }

            return color;
        }

        private Rectangle ComputeClippingRect(Element elem)
        {
            var rect = elem.Visibility == Visibility.Visible ? elem.BoundingBox : Rectangle.Empty;
            var p = elem.Parent;
            while (p != null)
            {
                rect = Rectangle.Intersect(rect, p.Visibility == Visibility.Visible ? p.BoundingBox : Rectangle.Empty);
                p = p.Parent;
            }

            // Translate the vectors into screen space.
            var pos = Scene.ViewportToScreen(rect.Location.ToVector2());
            var size = Scene.ViewportToScreen(rect.Size.ToVector2() + Vector2.One);

            rect.X = (int) pos.X;
            rect.Y = (int) pos.Y;
            rect.Width = (int) size.X;
            rect.Height = (int) size.Y;
            
            return rect;
        }

        private bool IsVisible(Element elem)
        {
            while (elem != null)
            {
                if (elem.Visibility != Visibility.Visible)
                    return false;
                elem = elem.Parent;
            }

            return true;
        }
        
        protected override void OnDraw(GameTime gameTime, Renderer batch)
        {
            base.OnDraw(gameTime, batch);

            foreach (var element in _rootElement.CollapseElements())
            {
                var opacity = ComputeElementOpacity(element);
                var masterTint = ComputeElementTint(element);
                var clip = ComputeClippingRect(element);
                
                // Save precious render time if the clipping rectangle is empty - the element isn't visible on-screen.
                if (clip.IsEmpty || !IsVisible(element))
                    continue;

                batch.SetScissorRectangle(clip);
                batch.Begin();
                
                var renderer = new GuiRenderer(batch, opacity, masterTint);

                element.Paint(gameTime, renderer);

                batch.End();

                batch.SetScissorRectangle(ComputeClippingRect(_rootElement));
                
                if (_debugShowBounds)
                {
                    var debugRenderer = new GuiRenderer(batch, 1, Color.White);

                    batch.Begin();

                    debugRenderer.DrawRectangle(element.BoundingBox, Color.White, 1);

                    var text = $"{element.Name}{Environment.NewLine}BoundingBox={element.BoundingBox}";
                    var measure = _debugFont.MeasureString(text);
                    var pos = new Vector2((element.BoundingBox.Left + ((element.BoundingBox.Width - measure.X) / 2)),
                        element.BoundingBox.Top + ((element.BoundingBox.Height - measure.Y) / 2));

                    debugRenderer.DrawString(_debugFont, text, pos, Color.White, TextAlign.Center, 2);
                    
                    
                    batch.End();
                }
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

                batch.Begin();

                batch.FillRectangle(new Rectangle((int)_tooltipPosition.X, (int)_tooltipPosition.Y, (int)measure.X, (int)measure.Y), Color.Black);

                batch.DrawString(font, wrapped, _tooltipPosition + new Vector2(5, 5), Color.White);

                batch.End();
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
    }
}