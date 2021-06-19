using System.Numerics;
using Thundershock.Core.Rendering;

namespace Thundershock.Core
{
    public static class RenderHelpers
    {
        public static void DrawString(this Renderer2D renderer, Font font, string text, Vector2 location, Color color)
        {
            font.Draw(renderer, text, location, color);
        }
    }
}