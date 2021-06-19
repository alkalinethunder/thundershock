using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Numerics;

namespace Thundershock.Core.Rendering
{
    /// <summary>
    /// The Sprite Rocket is an extremely enhanced version of the MonoGame Sprite Batch with heaps more
    /// options for renderable polygons.
    /// </summary>
    public sealed class Renderer2D
    {
        private Effect _effect;
        private bool _running;
        private List<RenderItem> _batch = new List<RenderItem>();
        private Texture2D _blankTexture;
        private Matrix4x4 _projectionMatrix = Matrix4x4.Identity;
        private Renderer _renderer;
        
        public Renderer2D(GraphicsProcessor gpu)
        {
            _renderer = new Renderer(gpu);
            
            _blankTexture = new Texture2D(gpu, 1, 1);
            _blankTexture.Upload(new byte[] {0xff, 0xff, 0xff, 0xff});
        }
        
        
        public void Begin(Matrix4x4? projection = null)
        {
            if (_running)
            {
                throw new InvalidOperationException("Cannot begin a new batch of sprites as the current batch has not been ended.");
            }

            _projectionMatrix = projection ?? Matrix4x4.Identity;
            
            _running = true;
            
            _batch.Clear();

            _effect = null;
        }
        
        /// <summary>
        /// Begins a new batch of polygons, using the given effect.
        /// </summary>
        /// <param name="effect">A pixel shader effect to apply to all polygons in the batch.</param>
        public void Begin(Effect effect, Matrix4x4? projection = null)
        {
            Begin(projection);
            _effect = effect;
        }

        private RenderItem MakeRenderItem(Texture2D texture, Matrix4x4? transform = null)
        {
            if (_running)
            {
                var tex = texture ?? _blankTexture;
                var mat = transform ?? Matrix4x4.Identity;
                
                if (_batch.Count > 0)
                {
                    var last = _batch[_batch.Count - 1];
                    if (last.Texture == tex && last.Transform == mat) return last;
                }

                var newBatchItem = new RenderItem();
                newBatchItem.Texture = tex;
                newBatchItem.Transform = mat;
                _batch.Add(newBatchItem);
                return newBatchItem;
            }
            else
            {
                throw new InvalidOperationException("You must call Begin() before you do this.");
            }
        }
        
        /// <summary>
        /// Ends the current batch and draws all polygons to the screen.
        /// </summary>
        /// <exception cref="InvalidOperationException">A batch hasn't begun yet.</exception>
        public void End()
        {
            if (_running)
            {
                _renderer.ProjectionMatrix = _projectionMatrix;
                _renderer.Begin(_effect);
                
                while (_batch.Count > 0)
                {
                    var item = _batch[0];
                    var vbo = item.Vertices;
                    var ibo = item.IndexBuffer;
                    var tex = item.Texture;

                    var pCount = item.Triangles;

                    if (pCount >= 1)
                    {
                        _renderer.Textures[0] = tex;

                        _renderer.Draw(PrimitiveType.TriangleStrip, vbo, ibo, 0, pCount);
                    }

                    _batch.RemoveAt(0);
                }

                _renderer.End();
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
            }
            
            var left = new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height);
            var right = new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height);
            var top = new Rectangle(left.Right, bounds.Top, bounds.Width - (thickness * 2), thickness);
            var bottom = new Rectangle(top.Left, bounds.Bottom - thickness, top.Width, top.Height);
            
            FillRectangle(left, color);
            FillRectangle(top, color);
            FillRectangle(right, color);
            FillRectangle(bottom, color);
        }

        public void FillRectangle(Rectangle rect, Color color, Texture2D texture, Rectangle uv, Matrix4x4 transform)
        {
            var batch = MakeRenderItem(texture, transform);

            var tl = batch.AddVertex(rect.Location, color, uv.Location);
            var tr = batch.AddVertex(new Vector2(rect.Right, rect.Top), color, new Vector2(uv.Right, uv.Top));
            var bl = batch.AddVertex(new Vector2(rect.Left, rect.Bottom), color, new Vector2(uv.Left, uv.Bottom));
            var br = batch.AddVertex(new Vector2(rect.Right, rect.Bottom), color, new Vector2(uv.Right, uv.Bottom));
            
            batch.AddIndex(tl);
            batch.AddIndex(tr);
            batch.AddIndex(bl);
            
            batch.AddIndex(bl);
            batch.AddIndex(tr);
            batch.AddIndex(br);
        }
        
        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to draw.</param>
        /// <param name="color">THe color of the rectangle.</param>
        /// <param name="texture">The texture to fill the area with.</param>
        public void FillRectangle(Rectangle rect, Color color, Texture2D texture = null)
        {
            var renderItem = MakeRenderItem(texture);

            // add the 4 vertices
            var tl = renderItem.AddVertex(new Vector2(rect.Left, rect.Top), color, TextureCoords.TopLeft);
            var tr = renderItem.AddVertex(new Vector2(rect.Right, rect.Top), color, TextureCoords.TopRight);
            var bl = renderItem.AddVertex(new Vector2(rect.Left, rect.Bottom), color, TextureCoords.BottomLeft);
            var br = renderItem.AddVertex(new Vector2(rect.Right, rect.Bottom), color, TextureCoords.BottomRight);

            // firsst triangle
            renderItem.AddIndex(tl);
            renderItem.AddIndex(tr);
            renderItem.AddIndex(bl);

            // second triangle
            renderItem.AddIndex(tr);
            renderItem.AddIndex(bl);
            renderItem.AddIndex(br);

            // And that's how you draw a rectangle with two triangles!
        }
        
        /* Credit where credit's due
         * =========================
         *
         * The following code is from https://github.com/Jjagg/OpenWheels,
         * and has been adapted to work within SpriteRocket2D.
         */
        
        private void CreateLine(Vector2 p1, Vector2 p2, Color color, float lineWidth, out int v1, out int v2, out int v3, out int v4)
        {
            var renderItem = MakeRenderItem(null);
            
            var d = Vector2.Normalize(p2 - p1);
            var dt = new Vector2(-d.Y, d.X) * (lineWidth / 2f);

            v1 = renderItem.AddVertex(p1 + dt, color,TextureCoords.TopLeft);
            v2 = renderItem.AddVertex(p1 - dt, color, TextureCoords.TopRight);
            v3 = renderItem.AddVertex(p2 - dt, color, TextureCoords.BottomRight);
            v4 = renderItem.AddVertex(p2 + dt, color, TextureCoords.BottomLeft);
        }
        
        private const float RightStartAngle = 0;
        private const float RightEndAngle = (float) (2 * Math.PI);

        /// <summary>
        /// Draws lines between two or more points, as if the game was playing Connect the Dots.
        /// </summary>
        /// <param name="points">A sequence of two or more points to draw lines between.</param>
        /// <param name="color">The color of each line.</param>
        /// <param name="lineWidth">The thickness of each line.</param>
        public void DrawLineStrip(ReadOnlySpan<Vector2> points, Color color, float lineWidth = 1)
        {
            if (points.Length < 2)
                return;

            var p1 = points[0];
            var p2 = points[1];

            CreateLine(p1, p2, color, lineWidth, out var i1, out var i2, out var i3, out var i4);

            var renderItem = MakeRenderItem(null);

            var i3Prev = i3;
            var i4Prev = i4;
            
            renderItem.AddIndex(i1);
            renderItem.AddIndex(i2);
            renderItem.AddIndex(i3);
            renderItem.AddIndex(i2);
            renderItem.AddIndex(i3);
            renderItem.AddIndex(i4);

            p1 = p2;
            
            for (var i = 2; i < points.Length; i++)
            {
                p2 = points[i];

                CreateLine(p1, p2, color, lineWidth, out i1, out i2, out i3, out i4);

                renderItem.AddIndex(i1);
                renderItem.AddIndex(i2);
                renderItem.AddIndex(i3);
                renderItem.AddIndex(i2);
                renderItem.AddIndex(i3);
                renderItem.AddIndex(i4);

                renderItem.AddIndex(i3Prev);
                renderItem.AddIndex(i4Prev);
                renderItem.AddIndex(i2);

                i3Prev = i3;
                i4Prev = i4;
                p1 = p2;
            }

        }
        
        private void FillTriangleFan(Vector2 center, ReadOnlySpan<Vector2> vs, Color color)
        {
            var c = vs.Length;
            if (c < 2)
                throw new ArgumentException(@"Need at least 3 vertices for a triangle fan.", nameof(vs));

            var renderItem = MakeRenderItem(null);

            var centerIndex = renderItem.AddVertex(center, color, Vector2.Zero);
            var v1 = renderItem.AddVertex(vs[0], color, Vector2.Zero);
            for (var i = 1; i < c; i++)
            {
                var v2 = renderItem.AddVertex(vs[i], color, Vector2.Zero);
                renderItem.AddIndex(centerIndex);
                renderItem.AddIndex(v1);
                renderItem.AddIndex(v2);
                v1 = v2;
            }
        }
        
        /// <summary>
        /// Draws a filled circle.
        /// </summary>
        /// <param name="center">The center point of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="color">The color to draw the circle with.</param>
        /// <param name="maxError">https://youtu.be/hQ3GW7lVBWY</param>
        public void FillCircle(Vector2 center, float radius, Color color, float maxError = .25f)
        {
            FillCircleSegment(center, radius, RightStartAngle, RightEndAngle, color, maxError);
        }
        
        private static void CreateCircleSegment(Vector2 center, float radius, float step, float start, float end, ref Span<Vector2> result)
        {
            var i = 0;
            float theta;
            for (theta = start; theta < end; theta += step)
                result[i++] = new Vector2((float) (center.X + radius * Math.Cos(theta)), (float) (center.Y + radius * Math.Sin(theta)));

            if (Math.Abs(theta - end) > 0.00001f)
                result[i] = center + new Vector2((float) (radius * Math.Cos(end)), (float) (radius * Math.Sin(end)));
        }
        
        private void FillCircleSegment(Vector2 center, float radius, float start, float end, Color color, float maxError)
        {
            ComputeCircleSegments(radius, maxError, end - start, out var step, out var segments);

            Span<Vector2> points = stackalloc Vector2[segments + 1];
            CreateCircleSegment(center, radius, step, start, end, ref points);
            
            FillTriangleFan(center, points, color);
        }
        
        private void ComputeCircleSegments(float radius, float maxError, float range, out float step, out int segments)
        {
            var invErrRad = 1 - maxError / radius;
            step = (float) Math.Acos(2 * invErrRad * invErrRad - 1);
            segments = (int) (range / step + 0.999f);
        }
        
        private class RenderItem
        {
            private int _vertexPointer;
            private int _indexPointer;
            private Vertex[] _vbo;
            private int[] _ibo;

            public int Triangles => _indexPointer / 3;
        
            public Texture2D Texture { get; set; }

            public Vertex[] Vertices => _vbo.AsSpan(0, _vertexPointer).ToArray();
            public int[] IndexBuffer => _ibo.AsSpan(0, _indexPointer).ToArray();

            public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;
            
            public RenderItem()
            {
                _vbo = new Vertex[128];
                _ibo = new int[128];
            }

            public void AddIndex(int index)
            {
                _ibo[_indexPointer] = index;
                _indexPointer++;
                if (_indexPointer >= _ibo.Length)
                    Array.Resize(ref _ibo, _ibo.Length * 2);
            }
        
            public int AddVertex(Vector2 position, Color color, Vector2 texCoord)
            {
                var i = _vertexPointer;

                if (i >= _vbo.Length)
                {
                    Array.Resize(ref _vbo, _vbo.Length * 2);
                }

                var pos3D = new Vector3(position, 0);
                pos3D = Vector3.Transform(pos3D, Transform);
                var v = new Vertex(pos3D, color, texCoord);
                _vbo[_vertexPointer] = v;
                _vertexPointer++;
                
                return i;
            }

        }

        private static class TextureCoords
        {
            public static Vector2 TopLeft => Vector2.Zero;
            public static Vector2 TopRight => new Vector2(1, 0);
            public static Vector2 BottomLeft => new Vector2(0, 1);
            public static Vector2 BottomRight => Vector2.One;
        }
    }
}