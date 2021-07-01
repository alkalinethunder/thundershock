using System.Numerics;
using System.Runtime.InteropServices;

namespace Thundershock.Core.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 Position;
        public Color Color;
        public Vector2 TextureCoordinates;

        public Vertex(Vector3 position, Color color, Vector2 texCoords)
        {
            Position = position;
            Color = color;
            TextureCoordinates = texCoords;
        }
    }
}