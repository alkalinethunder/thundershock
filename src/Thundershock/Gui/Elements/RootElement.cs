using System;
using System.Collections.Generic;

namespace Thundershock.Gui.Elements
{
    public sealed class RootElement : Element
    {
        protected override bool SupportsChildren => true;

        public LayoutManager RootLayoutManager
            => GetLayoutManager();

        internal RootElement(GuiSystem gui)
        {
            SetGuiSystem(gui ?? throw new ArgumentNullException(nameof(gui)));
        }
        
        public IEnumerable<Element> CollapseElements()
        {
            yield return this;

            foreach (var element in Children.Collapse())
                yield return element;
        }
    }
}