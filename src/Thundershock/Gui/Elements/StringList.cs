using System.Linq;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Gui.Styling;

namespace Thundershock.Gui.Elements
{
    public class StringList : ItemListElement<string>
    {
        public StyleFont ItemsFont { get; set; } = StyleFont.Default;

        public StyleColor BackColor { get; set; } = StyleColor.Default;
        public StyleColor ItemsColor { get; set; } = StyleColor.Default;
        public StyleColor ItemsActiveColor { get; set; } = StyleColor.Default;

        private Font GetFont()
        {
            return ItemsFont.GetFont(GuiSystem.Style.StringListFont);
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
            GuiSystem.Style.DrawStringListBackground(renderer, this);

            var pos = ContentRectangle.Location;
            var font = GetFont();
            
            for (var i = 0; i < Count; i++)
            {
                var item = this[i];

                var bounds = GetItemBounds(i, item);

                GuiSystem.Style.DrawListItem(renderer, this, bounds, i == SelectedIndex, i == HotIndex, item);
                
                pos.Y += font.LineSpacing;
            }
        }
    }
}