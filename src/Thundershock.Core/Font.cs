using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Thundershock.Core.Rendering;

namespace Thundershock.Core
{
    public abstract class Font
    {
        private GraphicsProcessor _gpu;

        public abstract int Size { get; set; }
        public abstract int LineSpacing { get; set; }
        public abstract int CharacterSpacing { get; set; }
        public abstract char DefaultCharacter { get; set; }
        
        public abstract Vector2 MeasureString(string text);
        public abstract void Draw(Renderer2D renderer, string text, Vector2 location, Color color);
        
        public static Font FromTtfStream(GraphicsProcessor gpu, Stream stream, int defaultSize = 16)
        {
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            return new FontStashFont(gpu, data, defaultSize);
        }


        public static Font GetDefaultFont(GraphicsProcessor gpu)
        {
            var result = false;

            if (Resource.GetStream(typeof(Font).Assembly, "Thundershock.Core.Resources.BuiltinFont.ttf", out Stream stream))
            {
                result = true;
                return FromTtfStream(gpu, stream);
            }

            Debug.Assert(false, "Couldn't find built-in engine font resource.");
            return null;
        }
    }
}