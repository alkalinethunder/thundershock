using Thundershock.Core;
using Thundershock.Gui.Styling;

namespace Thundershock.Gui.Elements
{
    public interface IButtonElement
    {
        public Rectangle BoundingBox { get; }
        public bool IsPressed { get; }
        public bool IsHovered { get; }
        public bool IsActive { get; set; }
        public StyleColor ButtonColor { get; set; }
        public StyleColor ButtonActiveColor { get; set; }
    }
}