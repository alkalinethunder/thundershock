using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using FontStashSharp.Interfaces;
using Thundershock.Core.Fonts;

namespace Thundershock.Core.Rendering
{
    /// <summary>
    /// The Sprite Rocket is an extremely enhanced version of the MonoGame Sprite Batch with heaps more
    /// options for renderable polygons.
    /// </summary>
    public sealed class Renderer2D
    {
        public readonly int MaxBatchCount = 1000;

        private int _sortLayer;
        private Rectangle _clipBounds;
        private int _batchPointer;
        private int _vertexPointer;
        private Vertex[] _vertexArray = new Vertex[1024];
        private Effect _effect;
        private bool _running;
        private RenderItem[] _batch = new RenderItem[16];
        private RenderItem[] _translucentBatch = new RenderItem[16];
        private Texture2D _blankTexture;
        private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
        private Renderer _renderer;
        private int _translucentBatchPointer = 0;
        private bool _enableSorting = true;

        public float Z => MaxBatchCount - _sortLayer;
        
        public bool EnableSorting
        {
            get => _enableSorting;
            set
            {
                if (_enableSorting != value)
                {
                    if (_running)
                        throw new InvalidOperationException("Cannot modify this property while rendering.");
                    _enableSorting = value;
                }
            }
        }

        public Matrix4x4 ProjectionMatrix
        {
            get => _projectionMatrix;
            set => _projectionMatrix = value;
        }

        public Renderer2D(GraphicsProcessor gpu)
        {
            _renderer = new Renderer(gpu);
            _blankTexture = new Texture2D(gpu, 1, 1, TextureFilteringMode.Point);
            _blankTexture.Upload(new byte[] { 0xff, 0xff, 0xff, 0xff });
        }


        public void Begin(Matrix4x4? projection = null)
        {
            if (_running)
            {
                throw new InvalidOperationException("Cannot begin a new batch of sprites as the current batch has not been ended.");
            }

            _translucentBatchPointer = 0;
            _running = true;
            _batchPointer = 0;
            _vertexPointer = 0;
            _effect = null;
        }

        public void SetClipBounds(Rectangle? bounds)
        {
            var realBounds = bounds ?? Rectangle.Empty;

            if (_clipBounds != realBounds)
            {
                var wasRunning = _running;
                var sort = _sortLayer;

                if (_running)
                    End();

                _renderer.Graphics.ScissorRectangle = realBounds;
                _renderer.Graphics.EnableScissoring = (realBounds.Width * realBounds.Height) > 0;
                _clipBounds = realBounds;

                if (wasRunning)
                {
                    Begin();
                    _sortLayer = sort;
                }
            }
        }

        private RenderItem MakeRenderItem(Texture2D texture, float alpha)
        {
            Debug.Assert(_running);
            var tex = texture ?? _blankTexture;

            // So here is where we find out why this method now needs
            // an alpha value....
            //
            // It's because we have two separate batches. One for opaque objects
            // and one for translucent ones.
            //
            // Renderer will draw them differently, but this technique lets us
            // submit one single index and vertex buffer.
            //
            if (alpha < 1)
            {
                for (var i = _translucentBatchPointer - 1; i >= 0; i--)
                {
                    var last = _translucentBatch[i];

                    Debug.Assert(last.IsOpaque == false);

                    if (last.Texture == tex)
                        return last;
                }
            }
            else
            {
                for (var i = _batchPointer - 1; i >= 0; i--)
                {
                    var last = _batch[i];

                    Debug.Assert(last.IsOpaque);

                    if (last.Texture == tex)
                        return last;
                }
            }

            var newBatchItem = null as RenderItem;

            if (alpha < 1)
            {
                // Make room for new batch items if we have run out.
                if (_translucentBatchPointer >= _translucentBatch.Length)
                    Array.Resize(ref _translucentBatch, _translucentBatchPointer + 16);

                newBatchItem = new RenderItem(this);
                newBatchItem.IsOpaque = false;
                _translucentBatch[_translucentBatchPointer] = newBatchItem;
                _translucentBatchPointer++;

                _translucentBatch[_translucentBatchPointer] = null;
            }
            else
            {
                // Make room for new batch items if we have run out.
                if (_batchPointer >= _batch.Length)
                    Array.Resize(ref _batch, _batchPointer + 16);

                newBatchItem = new RenderItem(this);
                newBatchItem.IsOpaque = true;

                _batch[_batchPointer] = newBatchItem;
                _batchPointer++;

                _batch[_batchPointer] = null;
            }

            newBatchItem.Texture = tex;

            return newBatchItem;
        }

        /// <summary>
        /// Ends the current batch and draws all polygons to the screen.
        /// </summary>
        /// <exception cref="InvalidOperationException">A batch hasn't begun yet.</exception>
        public void End()
        {
            if (_running)
            {
                if (_translucentBatchPointer + _batchPointer > 0)
                {
                    _renderer.ProjectionMatrix = _projectionMatrix;
                    _renderer.Begin(_effect);

                    // Submit the vertex buffer. All vertices for every single batch item are in here.
                    // This is a massive optimization since now we don't need to do this on every draw call.
                    _renderer.UploadVertices(_vertexArray.AsSpan(0, _vertexPointer));

                    for (var i = 0; i < _batchPointer; i++)
                    {
                        var item = _batch[i];
                        var tex = item.Texture;

                        // upload the indices.
                        _renderer.UploadIndices(item.Indices);

                        Debug.Assert(item.Length % 3 == 0);
                        Debug.Assert(item.Length / 3 == item.Triangles);

                        var pCount = item.Triangles;

                        if (pCount >= 1)
                        {
                            _renderer.Textures[0] = tex;

                            _renderer.Draw(PrimitiveType.TriangleList, item.Start, pCount);
                        }
                    }


                    for (var i = 0; i < _translucentBatchPointer; i++)
                    {
                        var item = _translucentBatch[i];
                        var tex = item.Texture;

                        // upload the indices.
                        _renderer.UploadIndices(item.Indices);

                        Debug.Assert(item.Length % 3 == 0);
                        Debug.Assert(item.Length / 3 == item.Triangles);

                        var pCount = item.Triangles;

                        if (pCount >= 1)
                        {
                            _renderer.Textures[0] = tex;

                            _renderer.Draw(PrimitiveType.TriangleList, item.Start, pCount);
                        }
                    }

                    _renderer.End();

                    // Reset the vertex and index pointers to give the impression that we've cleared
                    // the buffers.  We don't actually clear the buffers because resizing the arrays
                    // is a bit slow and unnecessary.
                    _vertexPointer = 0;
                    _batchPointer = 0;
                    _translucentBatchPointer = 0;

                    _batch[_batchPointer] = null;
                    _translucentBatch[_translucentBatchPointer] = null;

                }

                _sortLayer = 0;
            }
            else
            {
                throw new InvalidOperationException("Cannot end the current batch as it has not yet begun.");
            }

            _running = false;
        }

        /// <summary>
        /// Draws a rectangular outline.
        /// </summary>
        /// <param name="bounds">The area to draw the outline in.</param>
        /// <param name="color">The color of the outline.</param>
        /// <param name="thickness">The width of each line.</param>
        public void DrawRectangle(Rectangle bounds, Color color, int thickness)
        {
            if (thickness < 1)
                return;

            if (color.A <= 0)
                return;

            if (bounds.IsEmpty)
                return;

            if (bounds.Width <= thickness * 2 || bounds.Height <= thickness * 2)
            {
                FillRectangle(bounds, color);
                return;
            }

            var left = new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height);
            var right = new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height);
            var top = new Rectangle(left.Right, bounds.Top, bounds.Width - (thickness * 2), thickness);
            var bottom = new Rectangle(top.Left, bounds.Bottom - thickness, top.Width, top.Height);

            FillRectangle(left, color);
            _sortLayer--;
            FillRectangle(top, color);
            _sortLayer--;
            FillRectangle(right, color);
            _sortLayer--;
            FillRectangle(bottom, color);
        }

        public void FillRectangle(Rectangle rect, Color color, Texture2D texture, Rectangle uv)
        {
            var renderItem = MakeRenderItem(texture, color.A);

            Span<Vertex> vertices = ReserveQuad(renderItem);
            float z = MaxBatchCount - _sortLayer;
            vertices[0] = new Vertex(new Vector3(rect.Left, rect.Top, z), color, new Vector2(uv.Left, uv.Top));
            vertices[1] = new Vertex(new Vector3(rect.Right, rect.Top, z), color, new Vector2(uv.Right, uv.Top));
            vertices[2] = new Vertex(new Vector3(rect.Left, rect.Bottom, z), color, new Vector2(uv.Left, uv.Bottom));
            vertices[3] = new Vertex(new Vector3(rect.Right, rect.Bottom, z), color, new Vector2(uv.Right, uv.Bottom));

            IncreaseLayer();
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to draw.</param>
        /// <param name="color">THe color of the rectangle.</param>
        /// <param name="texture">The texture to fill the area with.</param>
        public void FillRectangle(Rectangle rect, Color color, Texture2D texture = null)
        {
            var renderItem = MakeRenderItem(texture, color.A);

            Span<Vertex> vertices = ReserveQuad(renderItem);
            float z = MaxBatchCount - _sortLayer;
            vertices[0] = new Vertex(new Vector3(rect.Left, rect.Top, z), color, TextureCoords.TopLeft);
            vertices[1] = new Vertex(new Vector3(rect.Right, rect.Top, z), color, TextureCoords.TopRight);
            vertices[2] = new Vertex(new Vector3(rect.Left, rect.Bottom, z), color, TextureCoords.BottomLeft);
            vertices[3] = new Vertex(new Vector3(rect.Right, rect.Bottom, z), color, TextureCoords.BottomRight);

            IncreaseLayer();
        }

        /* Credit where credit's due
         * =========================
         *
         * The following code is from https://github.com/Jjagg/OpenWheels,
         * and has been adapted to work within SpriteRocket2D.
         */

        private const float RightStartAngle = 0;
        private const float RightEndAngle = (float)(2 * Math.PI);

        private void FillTriangleFan(Vector2 center, Span<Vector2> vs, Color color, Texture2D texture)
        {
            var c = vs.Length;
            if (c < 2)
                throw new ArgumentException(@"Need at least 3 vertices for a triangle fan.", nameof(vs));

            var renderItem = MakeRenderItem(texture, color.A);
            Span<Vertex> vertexBuffer = ReserveVertices(1 + c, out int vertexPointer);
            Span<int> indexBuffer = renderItem.ReserveIndices((c - 1) * 3);

            var texWidth = texture?.Width ?? 1;
            var texHeight = texture?.Height ?? 1;
            var texDiameter = (float)Math.Min(texWidth, texHeight);
            var u = texDiameter / (float)texWidth;
            var v = texDiameter / (float)texHeight;

            var ul = (1f - u) / 2;
            var ut = (1f - v) / 2;

            var point5 = new Vector2(0.5f, 0.5f);
            var centerIndex = vertexPointer;
            vertexBuffer[0] = new Vertex(new Vector3(center, 0), color, point5);

            var rs = Vector2.Distance(center, vs[0]);

            var imageRect = Rectangle.FromHalfExtents(center, rs);
            var uvRect = new Rectangle(ul, ut, u, v);

            var uv1 = Rectangle.MapVec2(vs[0], imageRect, uvRect);

            int indexOffset = 0;
            var v1 = vertexPointer + 1;
            vertexBuffer[1] = new Vertex(new Vector3(vs[0], 0), color, uv1);

            for (var i = 1; i < c; i++)
            {
                var uv = Rectangle.MapVec2(vs[i], imageRect, uvRect);
                var v2 = vertexPointer + 1 + i;
                vertexBuffer[i + 1] = new Vertex(new Vector3(vs[i], 0), color, uv);
                indexBuffer[indexOffset++] = centerIndex;
                indexBuffer[indexOffset++] = v1;
                indexBuffer[indexOffset++] = v2;
                v1 = v2;
            }

            IncreaseLayer();
        }

        /// <summary>
        /// Draws a filled circle.
        /// </summary>
        /// <param name="center">The center point of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="color">The color to draw the circle with.</param>
        /// <param name="maxError">https://youtu.be/hQ3GW7lVBWY</param>
        public void FillCircle(Vector2 center, float radius, Color color, float maxError = 0.25f)
        {
            FillCircle(center, radius, color, null, maxError);
        }

        public void FillCircle(Vector2 center, float radius, Color color, Texture2D texture, float maxError = 0.25f)
        {
            FillCircleSegment(center, MathF.Round(radius), RightStartAngle, RightEndAngle, color, texture, maxError);
        }
        
        private static void CreateCircleSegment(Vector2 center, float radius, float step, float start, float end, Span<Vector2> result)
        {
            var i = 0;
            float theta;
            for (theta = start; theta < end; theta += step)
                result[i++] = new Vector2((float)(center.X + radius * Math.Cos(theta)), (float)(center.Y + radius * Math.Sin(theta)));

            if (theta != end)
                result[i] = center + new Vector2((float)(radius * Math.Cos(end)), (float)(radius * Math.Sin(end)));
        }

        private void FillCircleSegment(Vector2 center, float radius, float start, float end, Color color, Texture2D texture, float maxError)
        {
            if (radius <= 0)
                return;

            if (color.A <= 0)
                return;

            ComputeCircleSegments(radius, maxError, end - start, out var step, out var segments);

            if (segments <= 0)
                return;

            Span<Vector2> points = stackalloc Vector2[segments + 1];
            CreateCircleSegment(center, radius, step, start, end, points);

            FillTriangleFan(center, points, color, texture);
        }

        public void SetLayer(int layer)
        {
            _sortLayer = layer;
        }

        private void ComputeCircleSegments(float radius, float maxError, float range, out float step, out int segments)
        {
            var invErrRad = 1 - maxError / radius;
            step = (float)Math.Acos(2 * invErrRad * invErrRad - 1);
            segments = (int)(range / step + 0.999f);
        }

        internal void IncreaseLayer()
        {
            if (EnableSorting)
                _sortLayer++;
        }

        private class RenderItem
        {
            private int[] _indices = new int[1024];

            private Renderer2D _renderer;
            private int _indexPointer;

            public Span<int> Indices => _indices.AsSpan(0, _indexPointer);

            public int Triangles => _indexPointer / 3;

            public int Start => 0;

            public int Length => _indexPointer;

            public Texture2D Texture { get; set; }

            public bool IsOpaque { get; set; }

            public RenderItem(Renderer2D renderer)
            {
                _renderer = renderer;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<int> ReserveIndices(int count)
            {
                int indexPointer = _indexPointer;
                _indexPointer += count;

                // Make room if we've run out.
                if (indexPointer + count > _indices.Length)
                {
                    EnsureIndexCapacity(count);
                }

                Span<int> span = _indices.AsSpan(indexPointer, count);
                return span;
            }

            private void EnsureIndexCapacity(int count)
            {
                while (_indexPointer + count > _indices.Length)
                {
                    Array.Resize(ref _indices, _indices.Length * 2);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<Vertex> ReserveVertices(int count, out int vertexPointer)
        {
            vertexPointer = _vertexPointer;
            _vertexPointer += count;

            if (vertexPointer + count > _vertexArray.Length)
            {
                EnsureVertexCapacity(count);
            }

            Span<Vertex> span = _vertexArray.AsSpan(vertexPointer, count);
            return span;
        }

        private Span<Vertex> ReserveQuad(RenderItem batch)
        {
            Span<Vertex> vertices = ReserveVertices(4, out int vertexPointer);

            Span<int> indices = batch.ReserveIndices(6);
            indices[0] = vertexPointer + 1;
            indices[1] = vertexPointer + 2;
            indices[2] = vertexPointer + 0;
            indices[3] = vertexPointer + 1;
            indices[4] = vertexPointer + 3;
            indices[5] = vertexPointer + 2;

            return vertices;
        }

        private void EnsureVertexCapacity(int count)
        {
            while (_vertexPointer + count > _vertexArray.Length)
            {
                Array.Resize(ref _vertexArray, _vertexArray.Length * 2);
            }
        }

        private static class TextureCoords
        {
            public static Vector2 TopLeft => Vector2.Zero;
            public static Vector2 TopRight => new Vector2(1, 0);
            public static Vector2 BottomLeft => new Vector2(0, 1);
            public static Vector2 BottomRight => Vector2.One;
        }

        public void DrawText(TextRenderBuffer textData)
        {
            var color = textData.Color;
            
            for (var i = 0; i < textData.ItemCount; i++)
            {
                var item = textData.Items[i];

                var texture = item.Texture;

                var ri = MakeRenderItem(texture, color.A);
                
                // Reserve and copy over the needed vertices.
                var vertDestination = ReserveVertices(item.VertexCount, out var vertPtr);
                var vertSrc = item.Vertices.AsSpan(0, item.VertexCount);
                vertSrc.CopyTo(vertDestination);

                var numQuads = vertSrc.Length / 4;
                var numTriangles = numQuads * 2;
                var numIndices = numTriangles * 6;
                
                // Allocate the indices we need.
                var indices = ri.ReserveIndices(numIndices);
                
                for (var j = 0; j < numQuads; j++)
                {
                    var off = j * 4;
                    var vOff = vertPtr + off;

                    var iOff = j * 6;

                    indices[iOff] = vOff + 1;
                    indices[iOff + 1] = vOff + 2;
                    indices[iOff + 2] = vOff;

                    indices[iOff + 3] = vOff + 1;
                    indices[iOff + 4] = vOff + 3;
                    indices[iOff + 5] = vOff + 2;
                }
            }
            
            IncreaseLayer();
        }
    }
}