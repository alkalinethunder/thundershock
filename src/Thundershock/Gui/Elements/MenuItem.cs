using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Thundershock.Gui.Elements
{
    public class MenuItem
    {
        private MenuBar _menuBar;
        private string _name = "Menu item";
        private string _command = string.Empty;
        private bool _enabled = true;
        private string _icon = string.Empty;
        private string _tooltip = string.Empty;

        public class MenuItemCollection : ICollection<MenuItem>
        {
            private List<MenuItem> _items = new();
            private MenuItem _owner;

            public MenuItemCollection(MenuItem owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public IEnumerator<MenuItem> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            public void Add(MenuItem item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                if (item.Parent != null)
                    throw new InvalidOperationException(
                        "Menu item already belongs to a parent. We don't condone kidnapping.");

                if (item.MenuBar != null)
                    throw new InvalidOperationException("Menu item already belongs to a MenuBar.");

                item.Parent = _owner;
                _items.Add(item);
                _owner.BubbleUpdate();
            }

            public void Clear()
            {
                while (_items.Any())
                    Remove(_items.First());
            }

            public bool Contains(MenuItem item)
            {
                return item != null && item.Parent == _owner;
            }

            public void CopyTo(MenuItem[] array, int arrayIndex)
            {
                _items.CopyTo(array, arrayIndex);
            }

            public bool Remove(MenuItem item)
            {
                if (item == null)
                    return false;

                if (item.Parent != _owner)
                    return false;

                item.Parent = null;
                _items.Remove(item);
                _owner.BubbleUpdate();
                return true;
            }

            public int Count => _items.Count;
            public bool IsReadOnly => false;
        }

        public class MenuBarItemCollection : ICollection<MenuItem>
        {
            private List<MenuItem> _items = new();
            private MenuBar _owner;

            public MenuBarItemCollection(MenuBar owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public IEnumerator<MenuItem> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            public void Add(MenuItem item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                if (item.Parent != null)
                    throw new InvalidOperationException(
                        "Menu item already belongs to a parent. We don't condone kidnapping.");

                if (item.MenuBar != null)
                    throw new InvalidOperationException("Menu item already belongs to a MenuBar.");

                item._menuBar = _owner;
                _items.Add(item);
                _owner.Rebuild();
            }

            public void Clear()
            {
                while (_items.Any())
                    Remove(_items.First());
            }

            public bool Contains(MenuItem item)
            {
                return item != null && item.Parent == null && item._menuBar == _owner;
            }

            public void CopyTo(MenuItem[] array, int arrayIndex)
            {
                _items.CopyTo(array, arrayIndex);
            }

            public bool Remove(MenuItem item)
            {
                if (item == null)
                    return false;

                if (item.Parent != null)
                    return false;

                if (item._menuBar != _owner)
                    return false;

                item._menuBar = null;
                _items.Remove(item);
                _owner.Rebuild();
                return true;
            }

            public int Count => _items.Count;
            public bool IsReadOnly => false;
        }


        public string Text
        {
            get => _name;
            set
            {
                _name = value ?? string.Empty;
                BubbleUpdate();
            }
        }

        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value ?? string.Empty;
                BubbleUpdate();
            }
        }

        public string Command
        {
            get => _command;
            set
            {
                _command = value ?? string.Empty;
                BubbleUpdate();
            }
        }

        public string ToolTip
        {
            get => _tooltip;
            set
            {
                _tooltip = value ?? string.Empty;
                BubbleUpdate();
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                BubbleUpdate();
            }
        }

        public MenuItem Parent { get; private set; }

        public MenuBar MenuBar
        {
            get
            {
                if (Parent != null)
                    return Parent.MenuBar;
                return _menuBar;
            }
        }

        public MenuItemCollection Items { get; }

        public MenuItem()
        {
            Items = new MenuItemCollection(this);
        }

        private void BubbleUpdate()
        {
            if (Parent != null)
                Parent.BubbleUpdate();

            Updated?.Invoke(this, EventArgs.Empty);

            if (_menuBar != null)
                _menuBar.Rebuild();
        }

        public void Activate()
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Activated;
        public event EventHandler Updated;
    }
}
