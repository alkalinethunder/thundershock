using System;
using System.Numerics;
using FontStashSharp.Interfaces;
using Thundershock.Core.Rendering;

namespace Thundershock.Core.Fonts
{
    internal class FontStashRenderer : IFontStashRenderer
    {
        private Renderer2D _renderer;

        public FontStashRenderer(Renderer2D renderer)
        {
            _renderer = renderer;
        }
            
        public void Draw(object texture, Vector2 pos, System.Drawing.Rectangle? src, System.Drawing.Color color, float rotation, Vector2 origin, Vector2 scale,
            float depth)
        {
            var tsTexture = texture as Texture2D;

            var translate = Matrix4x4.CreateTranslation(-origin.X, -origin.Y, depth);
            var rotate = Matrix4x4.CreateRotationZ(rotation);
            var scaleMatrix = Matrix4x4.CreateScale(scale.X, scale.Y, 1);

            var transform = translate * scaleMatrix * rotate;
            
            var tsColor = new Color(color.R, color.G, color.B, color.A);

            var tsRectangle = Rectangle.Unit;
            var tsDrawRect = new Rectangle(0, 0, tsTexture.Width, tsTexture.Height);
            
            if (src != null)
            {
                tsRectangle.X = (float) src?.Left / tsTexture.Width;
                tsRectangle.Width = (float) src?.Width / tsTexture.Width;
                
                tsRectangle.Y = (float) src?.Top / tsTexture.Height;
                tsRectangle.Height = (float) src?.Height / tsTexture.Height;

                tsDrawRect.Width = (float) src?.Width;
                tsDrawRect.Height = (float) src?.Height;
            }

            tsDrawRect.X = pos.X;
            tsDrawRect.Y = pos.Y;
            
            _renderer.FillRectangle(tsDrawRect, tsColor, tsTexture, tsRectangle, transform);
        }
    }
}