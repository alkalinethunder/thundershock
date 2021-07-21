using System;
using System.Numerics;
using Thundershock.Gui.Elements;
using Thundershock.Core;

namespace Thundershock.Gui.Styling
{
    public abstract class GuiStyle
    {
        private GuiSystem _guiSystem;

        public virtual Color DefaultForeground { get; } = Color.Black;

        public virtual int ProgressBarHeight => 4;
        public virtual Padding CheckPadding => 4;
        
        protected GuiSystem Gui => _guiSystem;
        
        public abstract Font DefaultFont { get; }
        public abstract int CheckSize { get; }
        public abstract int TextCursorWidth { get; }
        
        public void Load(GuiSystem guiSystem)
        {
            if (_guiSystem != null)
                throw new InvalidOperationException("GUI style has already been bound.");
            
            _guiSystem = guiSystem ?? throw new ArgumentNullException(nameof(guiSystem));

            OnLoad();
        }
        
        public void Unload()
        {
            if (_guiSystem != null)
            {
                OnUnload();
                _guiSystem = null;
            }
        }
        
        protected virtual void OnLoad() {}
        protected virtual void OnUnload() {}

        public abstract void DrawSelectionBox(GuiRenderer renderer, Rectangle bounds, SelectionStyle selectionStyle);

        public abstract void DrawCheckBox(GuiRenderer renderer, CheckBox checkBox, Rectangle bounds);

        public abstract void DrawTextCursor(GuiRenderer renderer, Color color, Vector2 position, int height);

        public abstract void DrawButton(GuiRenderer renderer, IButtonElement button);

        public abstract void DrawStringListBackground(GuiRenderer renderer, StringList stringList);

        public abstract void DrawListItem(GuiRenderer renderer, StringList stringList, Rectangle bounds, bool isActive,
            bool isHovered, string text);

        public virtual void DrawProgressBar(GuiRenderer renderer, ProgressBar progressBar)
        {
            renderer.FillRectangle(progressBar.BoundingBox, Color.Gray);

            var contentBounds = progressBar.ContentRectangle;
            contentBounds.Width = (int) (contentBounds.Width * progressBar.Value);
            
            DrawSelectionBox(renderer, contentBounds, SelectionStyle.ItemActive);
        }
        
        public virtual Color GetButtonTextColor(IButtonElement button)
        {
            return Color.Black;
        }

        public virtual Font GetFont(Element element)
        {
            return DefaultFont;
        }

        public abstract void PaintElementBackground(Element element, GameTime gameTime, GuiRenderer renderer);
        
        public virtual void PaintMenuBar(MenuBar menuBar, GameTime gameTime, GuiRenderer renderer)
        {
            PaintElementBackground(menuBar, gameTime, renderer);
        }
        
        public virtual void PaintMenu(Menu menu, GameTime gameTime, GuiRenderer renderer)
        {
            PaintElementBackground(menu, gameTime, renderer);
        }

        public virtual void PaintMenuBarItemBackground(Element element, GameTime gameTime, GuiRenderer renderer,
            SelectionStyle selectionStyle)
        {
            DrawSelectionBox(renderer, element.BoundingBox, selectionStyle);
        }

        public abstract void PaintMenuItemText(Element element, GameTime gameTime, GuiRenderer renderer, string text,
            Font font,
            Vector2 textPos, SelectionStyle selectionStyle);
    }
}