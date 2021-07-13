using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
using Thundershock.Core.Rendering;

namespace Thundershock.Core
{
    public abstract class Font
    {
        public abstract int Size { get; set; }
        public abstract int LineSpacing { get; set; }
        public int LineHeight => Size + LineSpacing;
        public abstract int CharacterSpacing { get; set; }
        public abstract char DefaultCharacter { get; set; }
        
        public abstract Vector2 MeasureString(string text);
        public abstract void Draw(Renderer2D renderer, string text, Vector2 location, Color color);

        public static Font FromResource(GraphicsProcessor gpu, Assembly ass, string resource)
        {
            if (Resource.GetStream(ass, resource, out var stream))
                return FromTtfStream(gpu, stream);
            throw new InvalidOperationException("Resource not found.");
        }
        
        public static Font FromTtfStream(GraphicsProcessor gpu, Stream stream, int defaultSize = 16)
        {
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            return new FontStashFont(gpu, data, defaultSize);
        }


        public static Font GetDefaultFont(GraphicsProcessor gpu)
        {
            if (Resource.GetStream(typeof(Font).Assembly, "Thundershock.Core.Resources.BuiltinFont.ttf", out Stream stream))
            {
                return FromTtfStream(gpu, stream);
            }

            Debug.Assert(false, "Couldn't find built-in engine font resource.");
            return null;
        }
    }
}