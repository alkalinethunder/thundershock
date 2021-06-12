﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Gui.Elements;

namespace Thundershock.Gui.Styling
{
    public abstract class GuiStyle
    {
        private GuiSystem _guiSystem;

        public virtual Padding CheckPadding => 4;
        
        public virtual SpriteFont StringListFont => DefaultFont;
        public virtual SpriteFont ButtonFont => _guiSystem.FallbackFont;
        
        protected GuiSystem Gui => _guiSystem;
        
        public abstract SpriteFont DefaultFont { get; }
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
        
        public virtual Color GetButtonTextColor(IButtonElement button)
        {
            return Color.Black;
        }
    }
}