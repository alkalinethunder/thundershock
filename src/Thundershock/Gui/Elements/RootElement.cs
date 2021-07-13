using System;
using System.Collections.Generic;
using System.Numerics;

namespace Thundershock.Gui.Elements
{
    public sealed class RootElement : FreePanel
    {
        protected override bool SupportsChildren => true;

        public LayoutManager RootLayoutManager
            => GetLayoutManager();
        
        internal RootElement(GuiSystem gui)
        {
            SetGuiSystem(gui ?? throw new ArgumentNullException(nameof(gui)));
            DefaultAnchor = CanvasAnchor.Fill;
            DefaultAutoSize = true;
            DefaultAlignment = Vector2.Zero;
        }
        
        public IEnumerable<Element> CollapseElements()
        {
            if (Visibility == Visibility.Visible)
                yield return this;

            foreach (var element in Children.Collapse())
                yield return element;
        }
    }
}