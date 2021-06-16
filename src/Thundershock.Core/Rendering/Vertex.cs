using System.Numerics;

namespace Thundershock.Core.Rendering
{
    public class Vertex
    {
        public Vector3 Position { get; set; }
        public Color Color { get; set; }
        public Vector2 TextureCoordinates { get; set; }

        public Vertex(Vector3 position, Color color, Vector2 texCoords)
        {
            Position = position;
            Color = color;
            TextureCoordinates = texCoords;
        }
    }
}