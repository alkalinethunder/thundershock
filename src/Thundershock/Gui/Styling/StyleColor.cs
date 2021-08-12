using Thundershock.Core;
using Thundershock.Core.Scripting;

namespace Thundershock.Gui.Styling
{
    [ScriptType]
    public class StyleColor
    {
        private Color _realColor;
        private bool _isDefault;

        public bool IsDefault => _isDefault;
        public Color Color => _realColor;

        private StyleColor()
        {
            _isDefault = true;
            _realColor = Color.White;
        }

        public StyleColor(Color color)
        {
            _isDefault = false;
            _realColor = color;
        }

        public static implicit operator StyleColor(Color color)
        {
            return new(color);
        }
        
        public static implicit operator StyleColor(string htmlColor)
        {
            return FromHtml(htmlColor);
        }

        public Color GetColor(Color defaultColor)
        {
            if (IsDefault)
                return defaultColor;
            return _realColor;
        }
        
        public static StyleColor Default => new StyleColor();

        public static StyleColor FromLinearColor(Color color)
        {
            return new(color);
        }

        public static StyleColor FromHtml(string html)
        {
            return FromLinearColor(Core.Color.FromHtml(html));
        }
    }
}