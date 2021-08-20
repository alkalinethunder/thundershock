using System;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FontStashSharp.Interfaces;
using Thundershock.Core.Fonts;

namespace Thundershock.Core.Rendering
{
    public class TextRenderBufferItem
    {
        private Vertex[] _vertexArray = new Vertex[128];
        private int _vertexPointer;
        
        public Texture2D Texture { get; }
        public Vertex[] Vertices => _vertexArray;
        public int VertexCount => _vertexPointer;

        public TextRenderBufferItem(Texture2D texture)
        {
            Texture = texture;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<Vertex> ReserveVertices(int count, out int vertexPointer)
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
        
        private void EnsureVertexCapacity(int count)
        {
            while (_vertexPointer + count > _vertexArray.Length)
            {
                Array.Resize(ref _vertexArray, _vertexArray.Length * 2);
            }
        }

        
    }

    public class TextRenderBuffer : IFontStashRenderer
    {
        private FontTextureManager _textureManager;
        private TextRenderBufferItem[] _items = new TextRenderBufferItem[4];
        private int _itemPointer = 0;
        public TextRenderBufferItem[] Items => _items;

        public int ItemCount => _itemPointer;
        
        public Color Color { get; set; }
        public Vector2 Location { get; }
        
        internal TextRenderBuffer(FontTextureManager textureManager, Vector2 location, Color color)
        {
            this.Color = color;
            this.Location = location;
            _textureManager = textureManager;
        }

        private TextRenderBufferItem MakeItem(Texture2D texture)
        {
            if (_itemPointer > 0)
            {
                if (_items[_itemPointer - 1].Texture == texture)
                    return _items[_itemPointer - 1];
            }

            var item = new TextRenderBufferItem(texture);
            
            while (_itemPointer >= _items.Length)
                Array.Resize(ref _items, _items.Length * 2);

            _items[_itemPointer] = item;
            _itemPointer++;

            return item;
        }

        public void Draw(object texture, Vector2 pos, System.Drawing.Rectangle? src, System.Drawing.Color color,
            float rotation, Vector2 origin, Vector2 scale,
            float depth)
        {
            Debug.Assert(texture is Texture2D);
            
            var tsTexture = texture as Texture2D;
            
            var location = new Vector3((pos - (origin * scale)) + Location, depth);

            var rect = new Rectangle(location.X, location.Y, (float) src?.Width, (float) src?.Height);
            var uvRect = rect;
            uvRect.X = (float) src?.X / tsTexture.Width;
            uvRect.Y = (float) src?.Y / tsTexture.Height;
            uvRect.Width /= tsTexture.Width;
            uvRect.Height /= tsTexture.Height;

            rect.Width *= scale.X;
            rect.Height *= scale.Y;

            var renderItem = MakeItem(tsTexture);

            var quad = renderItem.ReserveVertices(4, out var ptr);

            quad[0].Position = location;
            quad[0].Color = Color;
            quad[0].TextureCoordinates = uvRect.Location;
            quad[1].Position = new Vector3(rect.Right, location.Y, depth);
            quad[1].Color = Color;
            quad[1].TextureCoordinates = new Vector2(uvRect.Right, uvRect.Top);
            quad[2].Position = new Vector3(location.X, rect.Bottom, depth);
            quad[2].Color = Color;
            quad[2].TextureCoordinates = new Vector2(uvRect.Left, uvRect.Bottom);
            quad[3].Position = new Vector3(rect.Right, rect.Bottom, depth);
            quad[3].Color = Color;
            quad[3].TextureCoordinates = new Vector2(uvRect.Right, uvRect.Bottom);
        }

        public ITexture2DManager TextureManager => _textureManager;
    }

}