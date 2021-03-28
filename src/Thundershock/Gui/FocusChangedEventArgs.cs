using System;
using Thundershock.Gui.Elements;

namespace Thundershock.Gui
{
    public class FocusChangedEventArgs : EventArgs
    {
        public Element Focused { get; }
        public Element Blurred { get; }

        public FocusChangedEventArgs(Element blurred, Element focused)
        {
            Blurred = blurred;
            Focused = focused;
        }
    }
}