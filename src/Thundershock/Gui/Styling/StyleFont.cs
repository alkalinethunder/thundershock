using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Gui.Styling
{
    public class StyleFont
    {
        private bool _isDefault;
        private SpriteFont _realFont;

        private StyleFont()
        {
            _isDefault = true;
            _realFont = null;
        }

        public StyleFont(SpriteFont font)
        {
            _isDefault = false;
            _realFont = font;
        }
        
        public static implicit operator StyleFont(SpriteFont font)
        {
            return new StyleFont(font);
        }
        
        public static StyleFont Default => new StyleFont();
        
        public SpriteFont GetFont(SpriteFont defaultFont)
        {
            if (_isDefault)
                return defaultFont;
            return _realFont;
        }
    }
}