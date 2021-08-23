using System.Numerics;
using FontStashSharp;
using Thundershock.Core.Fonts;
using Thundershock.Core.Rendering;

namespace Thundershock.Core
{
    internal class FontStashFont : Font
    {
        private static FontTextureManager _textureManager;
        
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
            // Create the texture manager if it doesn't already exist.
            if (_textureManager == null)
            {
                _textureManager = new FontTextureManager(gpu);
            }
            
            // TODO: Dynamic font quality settings.
            var settings = new FontSystemSettings
            {
                FontResolutionFactor = 2.0f,
                KernelWidth = 2,
                KernelHeight = 2,
                PremultiplyAlpha = false,
                TextureWidth = 1024,
                TextureHeight = 1024
            };
            
            _fontSystem = new FontSystem(settings);

            _fontSystem.UseKernings = true;

            _fontSystem.AddFont(ttfData);
            _fontSize = defaultSize;
        }

        public override TextRenderBuffer Draw(string text, Vector2 location, Color color, float z)
        {
            var textBuffer = new TextRenderBuffer(_textureManager, location, color, z);
            var font = _fontSystem.GetFont(_fontSize);
            font.DrawText(textBuffer, text, Vector2.Zero, Color.White, layerDepth: z);
            return textBuffer;
        }

        public override void Draw(TextRenderBuffer existingBuffer, string text, Vector2 location, Color color, float z)
        {
            existingBuffer.Color = color;
            
            var font = _fontSystem.GetFont(_fontSize);
            var pos = location - existingBuffer.Location;
            font.DrawText(existingBuffer, text, pos, Color.White, layerDepth: z);
        }

        public override TextRenderBuffer DrawLines(string[] lines, Vector2 location, Color color, float z)
        {
            var buffer = new TextRenderBuffer(_textureManager, location, color, z);
            var font = _fontSystem.GetFont(_fontSize);
            var pos = Vector2.Zero;
            
            foreach (var line in lines)
            {
                font.DrawText(buffer, line, pos, color, layerDepth: z);                
                pos.Y += LineSpacing;
            }

            return buffer;
        }

        public override Vector2 MeasureString(string text)
        {
            return _fontSystem.GetFont(_fontSize).MeasureString(text);
        }
    }
}