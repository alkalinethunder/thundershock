using System.Numerics;
using Thundershock.Core.Rendering;

namespace Thundershock.Core
{
    public static class RenderHelpers
    {
        public static TextRenderBuffer DrawString(this Renderer2D renderer, Font font, string text, Vector2 location, Color color)
        {
            var buffer = font.Draw(text, location, color, renderer.Z);

            renderer.DrawText(buffer);

            return buffer;
        }
    }
}