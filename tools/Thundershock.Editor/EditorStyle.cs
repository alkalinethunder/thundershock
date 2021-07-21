using System;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Gui;
using Thundershock.Gui.Elements;
using Thundershock.Gui.Styling;

namespace Thundershock.Editor
{
    public sealed class EditorStyle : GuiStyle
    {
        private Color _bg = ThundershockPlatform.HtmlColor("#161816");
        private Color _peace = ThundershockPlatform.HtmlColor("#1baaf7");
        private Font _font;
        
        public override Font DefaultFont => _font;
        public override int CheckSize => 18;
        public override int TextCursorWidth => 1;

        protected override void OnLoad()
        {
            _font = Font.GetDefaultFont(this.Gui.Graphics);
        }

        public override void DrawSelectionBox(GuiRenderer renderer, Rectangle bounds, SelectionStyle selectionStyle)
        {
            var color = selectionStyle switch
            {
                SelectionStyle.None => Color.Transparent,
                SelectionStyle.TextHighlight => Color.Blue,
                SelectionStyle.ItemHover => _peace * 0.5f,
                SelectionStyle.ItemActive => _peace,
                _ => Color.Transparent
            };
            
            renderer.FillRectangle(bounds, color);
        }

        public override void DrawCheckBox(GuiRenderer renderer, CheckBox checkBox, Rectangle bounds)
        {
            
        }

        public override void DrawTextCursor(GuiRenderer renderer, Color color, Vector2 position, int height)
        {
        }

        public override void DrawButton(GuiRenderer renderer, IButtonElement button)
        {
        }

        public override void DrawStringListBackground(GuiRenderer renderer, StringList stringList)
        {
        }

        public override void DrawListItem(GuiRenderer renderer, StringList stringList, Rectangle bounds, bool isActive, bool isHovered,
            string text)
        {
        }

        public override void PaintElementBackground(Element element, GameTime gameTime, GuiRenderer renderer)
        {
            renderer.FillRectangle(element.BoundingBox, _bg);
        }

        public override void PaintMenuItemText(Element element, GameTime gameTime, GuiRenderer renderer, string text, Font font,
            Vector2 textPos, SelectionStyle selectionStyle)
        {
            renderer.DrawString(font, text, textPos, Color.White);
        }
    }
}