using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Gui.Styling;

namespace Thundershock.Gui.Elements
{
    public class StringList : ItemListElement<string>
    {
        public SpriteFont ItemsFont { get; set; }

        private SpriteFont GetFont()
        {
            return ItemsFont ?? GuiSystem.Style.DefaultFont;
        }

        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            if (Count <= 0)
                return Vector2.Zero;

            var longest = Items.OrderByDescending(x => x.Length).First();

            var font = GetFont();

            var m = font.MeasureString(longest);

            m.Y = font.LineSpacing * Count;

            return m;
        }

        protected override Rectangle GetItemBounds(int index, string value)
        {
            var font = GetFont();
            
            var rect = ContentRectangle;
            rect.Height = font.LineSpacing;
            rect.Y += font.LineSpacing * index;

            return rect;
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            var pos = ContentRectangle.Location.ToVector2();
            var font = GetFont();
            
            for (var i = 0; i < Count; i++)
            {
                var item = this[i];

                var bounds = GetItemBounds(i, item);

                if (i == SelectedIndex)
                {
                    GuiSystem.Style.DrawSelectionBox(renderer, bounds, SelectionStyle.ItemActive);
                }
                else if (i == HotIndex)
                {
                    GuiSystem.Style.DrawSelectionBox(renderer, bounds, SelectionStyle.ItemHover);
                }
                
                renderer.DrawString(font, item, pos, Color.Black);

                pos.Y += font.LineSpacing;
            }
        }
    }
}