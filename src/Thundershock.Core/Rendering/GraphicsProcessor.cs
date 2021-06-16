namespace Thundershock.Core.Rendering
{
    public abstract class GraphicsProcessor
    {
        public abstract void Clear(Color color);

        public abstract void DrawIndexedPrimitives(PrimitiveType type, Vertex[] vertices, int[] indices, int indexStart,
            int primitiveCount);
    }
}