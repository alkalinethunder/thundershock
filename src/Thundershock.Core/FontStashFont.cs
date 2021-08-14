using System.Numerics;
using FontStashSharp;
using Thundershock.Core.Fonts;
using Thundershock.Core.Rendering;

namespace Thundershock.Core
{
    internal class FontStashFont : Font
    {
        private FontSystem _fontSystem;
        private int _fontSize;
        
        public override int Size
        {
            get => _fontSize;
            set => _fontSize = value;
        }

        public override int LineSpacing
        {
            get => _fontSystem.LineSpacing;
            set => _fontSystem.LineSpacing = value;
        }

        public override int CharacterSpacing
        {
            get => _fontSystem.CharacterSpacing;
            set => _fontSystem.CharacterSpacing = value;
        }

        public override char DefaultCharacter
        {
            get => (char?) _fontSystem.DefaultCharacter ?? '\0';
            set => _fontSystem.DefaultCharacter = value;
        }
        
        public FontStashFont(GraphicsProcessor gpu, byte[] ttfData, int defaultSize)
        {
            var texManager = new FontTextureManager(gpu);
            _fontSystem = new FontSystem(texManager, 1024, 1024, 0, 0, false);

            _fontSystem.UseKernings = true;
            
            _fontSystem.AddFont(ttfData);
            _fontSize = defaultSize;
        }

        public override void Draw(Renderer2D renderer, string text, Vector2 location, Color color)
        {
            var font = _fontSystem.GetFont(_fontSize);
            font.DrawText(renderer, text, location, color);
            renderer.IncreaseLayer();
        }

        public override Vector2 MeasureString(string text)
        {
            return _fontSystem.GetFont(_fontSize).MeasureString(text);
        }
    }
}