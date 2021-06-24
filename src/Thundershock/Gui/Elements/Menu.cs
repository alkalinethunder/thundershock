using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Input;

namespace Thundershock.Gui.Elements
{
    public class Menu : LayoutElement
    {
        private MenuItem _item;

        private List<Menu> _submenus = new();

        private Panel _bg = new();
        private Stacker _menuList = new();

        public MenuItem Item => _item;

        public Menu(MenuItem item)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));

            _menuList.Padding = 2;
            _bg.Children.Add(_menuList);
            Children.Add(_bg);

            Build();

            _item.Updated += ItemOnUpdated;
        }

        ~Menu()
        {
            _item.Updated -= ItemOnUpdated;
        }

        private void ItemOnUpdated(object? sender, EventArgs e)
        {
            Build();
        }

        public void Close()
        {
            foreach (var submenu in _submenus)
                submenu.Close();

            if (Parent != null)
            {
                Parent.Children.Remove(this);
            }
        }

        public void Open()
        {
            Close();

            _item.Activate();
        }

        private void Build()
        {
            Close();

            _submenus.Clear();
            _menuList.Children.Clear();

            foreach (var item in _item.Items)
            {
                var menu = new Menu(item);
                var button = new Button();

                button.Enabled = item.Enabled;

                button.Text = item.Text;
                button.Properties.SetValue("menu", menu);

                // Set up the menu canvas settings
                menu.Properties.SetValue(FreePanel.AutoSizeProperty, true);
                menu.Properties.SetValue(FreePanel.AnchorProperty, FreePanel.CanvasAnchor.TopLeft);

                // Activations
                button.MouseEnter += MenuItemMouseEnter;
                button.MouseUp += ButtonOnMouseUp;
                _submenus.Add(menu);
                _menuList.Children.Add(button);
            }
        }

        private void ButtonOnMouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Primary)
            {
                if (sender is Button button)
                {
                    // close all submenus
                    foreach (var submenu in _submenus)
                        submenu.Close();

                    var menu = button.Properties.GetValue<Menu>("menu");
                    menu.Open();
                }
            }
        }

        private void MenuItemMouseEnter(object? sender, MouseMoveEventArgs e)
        {
            if (sender is Button button)
            {
                // close all submenus
                foreach (var submenu in _submenus)
                    submenu.Close();

                var menu = button.Properties.GetValue<Menu>("menu");

                // open the submenu WITHOUT activating the item.
                GuiSystem.AddToViewport(menu);
            }
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            base.ArrangeOverride(contentRectangle);

            // This works a lot like MenuBar does except that we align the menus a little differently.
            foreach (var button in _menuList.Children.OfType<Button>())
            {
                var menu = button.Properties.GetValue<Menu>("menu");

                var size = menu.ActualSize;

                var buttRect = button.BoundingBox;

                var pos = new Vector2(buttRect.Right, buttRect.Top);

                var menuRect = new Rectangle((int) pos.X, (int) pos.Y, (int) ActualSize.X, (int) ActualSize.Y);

                // So for the menu rectangle we want to avoid overlapping the parent item if at all possible.
                // So if the right side of menuRect is outside of the viewport then we're going to try alignning it
                // to the left of the parent item.
                if (menuRect.Right > GuiSystem.BoundingBox.Right)
                {
                    menuRect.X = buttRect.Left - menuRect.Width;
                }

                // But now there's a chance that the left side of the menu will be cut off.
                // If that's the case then we cannot possibly stop ourselves from overlapping.
                // So we'll align the item to the right.
                if (menuRect.Left < GuiSystem.BoundingBox.Left)
                {
                    menuRect.X = buttRect.Right - menuRect.Width;
                }

                // Now handle the bottom of the menu.
                if (menuRect.Bottom > GuiSystem.BoundingBox.Bottom)
                {
                    var delta = menuRect.Bottom - GuiSystem.BoundingBox.Bottom;
                    menuRect.Y -= delta;
                }

                // Now we can position the menu.
                menu.Properties.SetValue(FreePanel.PositionProperty, menuRect.Location);
            }
        }
    }
}
