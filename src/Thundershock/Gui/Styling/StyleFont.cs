using Thundershock.Core;

namespace Thundershock.Gui.Styling
{
    public class StyleFont
    {
        private bool _isDefault;
        private Font _realFont;

        private StyleFont()
        {
            _isDefault = true;
            _realFont = null;
        }

        public StyleFont(Font font)
        {
            _isDefault = false;
            _realFont = font;
        }
        
        public static implicit operator StyleFont(Font font)
        {
            return new StyleFont(font);
        }
        
        public static StyleFont Default => new StyleFont();
        
        public Font GetFont(Font defaultFont)
        {
            if (_isDefault)
                return defaultFont;
            return _realFont;
        }
    }
}