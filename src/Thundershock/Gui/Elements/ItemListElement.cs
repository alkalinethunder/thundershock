using System;
using System.Collections.Generic;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Input;

namespace Thundershock.Gui.Elements
{
    public abstract class ItemListElement<T> : ContentElement
    {
        private readonly List<T> _items = new();
        private int _selectedItem = -1;
        private int _hotItem = -1;
        
        public event EventHandler SelectedIndexChanged;

        protected int HotIndex => HotTracking ? _hotItem : -1;
        
        public bool HotTracking { get; set; } = true;

        public ItemListElement()
        {
            CanFocus = true;
            IsInteractable = true;
        }
        
        public int SelectedIndex
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    if (value < -1 && value >= _items.Count)
                        throw new ArgumentOutOfRangeException(nameof(value), value,
                            "Selected index must be -1 for no selection or between 0 and the number of items in the list.");
                    
                    _selectedItem = value;
                    OnSelectedIndexChanged(value);
                }
            }
        }

        public int Count => _items.Count;
        
        public T SelectedItem => _selectedItem == -1 ? default : _items[_selectedItem];

        public IEnumerable<T> Items => _items;
        
        public T this[int index]
        {
            get => _items[index];
            set
            {
                _items[index] = value;
            }
        }
        
        protected virtual void OnSelectedIndexChanged(int value)
        {
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            _items.Clear();
            _selectedItem = -1;
        }

        public void AddItem(T value)
        {
            if (!_items.Contains(value))
            {
                _items.Add(value);
            }
        }

        public bool RemoveItem(T value)
        {
            if (_items.Contains(value))
            {
                var i = _items.IndexOf(value);

                _items.Remove(value);
                if (SelectedIndex >= i)
                {
                    SelectedIndex--;
                }
                
                return true;
            }

            return false;
        }

        protected override bool OnMouseMove(MouseMoveEventArgs e)
        {
            if (HotTracking)
            {
                var pos = GuiSystem.ScreenToViewport(new Vector2(e.X, e.Y));

                if (TryGetItem((int) pos.X, (int) pos.Y,  out int index))
                {
                    _hotItem = index;
                }
                else
                {
                    _hotItem = -1;
                }
            }
            
            return base.OnMouseMove(e);
        }

        protected override bool OnMouseLeave(MouseMoveEventArgs e)
        {
            if (HotTracking)
            {
                _hotItem = -1;
            }
            
            return base.OnMouseLeave(e);
        }

        protected override bool OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Primary)
            {
                var pos = GuiSystem.ScreenToViewport(new Vector2(e.X, e.Y));

                if (TryGetItem((int) pos.X, (int) pos.Y, out int index))
                {
                    SelectedIndex = index;
                    return true;
                }
            }

            return base.OnMouseUp(e);
        }

        public void InsertItem(int index, T value)
        {
            if (index < 0 || index > _items.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _items.Insert(index, value);
            
            if (SelectedIndex > index)
            {
                SelectedIndex++;
            }
        }

        protected abstract Rectangle GetItemBounds(int index, T value);
        
        public bool TryGetItem(int x, int y, out int index)
        {
            if (x >= BoundingBox.Left && x <= BoundingBox.Right && y >= BoundingBox.Top && y <= BoundingBox.Bottom)
            {
                for (var i = 0; i < Count; i++)
                {
                    var item = this[i];
                    var bounds = GetItemBounds(i, item);

                    if (x >= bounds.Left && x <= bounds.Right && y >= bounds.Top && y <= bounds.Bottom)
                    {
                        index = i;
                        return true;
                    }
                }
            }

            index = -1;
            return false;
        }

        public int IndexOf(T value)
        {
            return _items.IndexOf(value);
        }
    }
}