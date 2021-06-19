using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Input;

namespace Thundershock.Gui.Elements
{
    public class MenuBar : Element
    {
        private Panel _menuBackground = new();
        private Stacker _menuStacker = new();
        private List<Menu> _menus = new();
        private static readonly string MenuBarItemTag = "menubar.item";
        private static readonly string MenuBarMenuTag = "menubar.menu";

        public MenuItem.MenuBarItemCollection Items { get; }

        public MenuBar()
        {
            Items = new(this);

            _menuStacker.Direction = StackDirection.Horizontal;
            _menuStacker.Padding = new Padding(2, 4);

            _menuBackground.Children.Add(_menuStacker);
            Children.Add(_menuBackground);
        }

        public void Rebuild()
        {
            // Close all menu items.
            foreach (var menu in _menus)
                menu.Close();

            _menus.Clear();
            _menuStacker.Children.Clear();

            foreach (var item in Items)
            {
                if (!item.Enabled)
                    continue;

                var button = new Button();
                button.Properties.SetValue(MenuBarItemTag, item);
                button.Text = item.Text;
                button.Padding = new Padding(1, 0);
                _menuStacker.Children.Add(button);

                button.MouseUp += MenuButtonOnMouseUp;

                var menu = new Menu(item);
                button.Properties.SetValue(MenuBarMenuTag, menu);

                // Set up the menu canvas positioning since it's a top-level.
                menu.Properties.SetValue(FreePanel.AutoSizeProperty, true);
                menu.Properties.SetValue(FreePanel.AnchorProperty, FreePanel.CanvasAnchor.TopLeft);

                _menus.Add(menu);
            }
        }

        private void MenuButtonOnMouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Primary)
            {
                if (sender is Button button)
                {
                    var menu = button.Properties.GetValue<Menu>(MenuBarMenuTag);
                    menu.Open();

                    GuiSystem.AddToViewport(menu);
                }
            }
        }

        protected override void ArrangeOverride(Rectangle contentRectangle)
        {
            base.ArrangeOverride(contentRectangle);

            // This is where we get to go through our buttons
            // and position the menus they're associated with.
            foreach (var button in _menuStacker.Children.OfType<Button>())
            {
                var menu = button.Properties.GetValue<Menu>(MenuBarMenuTag);

                // Get the actual size of the menu.
                var actSize = menu.ActualSize;

                // Get the button rectangle
                var buttRect = button.BoundingBox;

                // Position
                var pos = new Vector2(buttRect.Left, buttRect.Bottom);

                // Desired rectangle for the menu.
                var menuRect = new Rectangle((int) pos.X, (int) pos.Y, (int) actSize.X, (int) actSize.Y);

                // Check if the right side is outside the viewport bounds.
                if (menuRect.Right > GuiSystem.BoundingBox.Right)
                {
                    // Shift it into place.
                    var delta = menuRect.Right - GuiSystem.BoundingBox.Right;
                    menuRect.X -= delta;
                }

                // Do the same for the bottom of the menu.
                if (menuRect.Bottom > GuiSystem.BoundingBox.Bottom)
                {
                    var delta = menuRect.Bottom - GuiSystem.BoundingBox.Bottom;
                    menuRect.Y -= delta;
                }

                // We now know where the menu should be.
                menu.Properties.SetValue(FreePanel.PositionProperty, new Vector2(menuRect.Left, menuRect.Top));
            }
        }
    }
}
