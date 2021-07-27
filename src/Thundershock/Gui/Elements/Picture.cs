using Thundershock.Core;
using Thundershock.Core.Rendering;
using System.Numerics;

namespace Thundershock.Gui.Elements
{
    public class Picture : ContentElement
    {
        private Texture2D _image;
        
        public Color Tint { get; set; } = Color.White;

        public Texture2D Image
        {
            get => _image;
            set => _image = value;
        }

        public ImageMode ImageMode { get; set; } = ImageMode.Fit;
        
        protected override Vector2 MeasureOverride(Vector2 alottedSize)
        {
            if (Image == null)
                return Vector2.Zero;
            
            if (alottedSize.X > 0)
            {
                // if max size x is positive then I need to know the scaled height of the image based on
                // the image aspect ratio and the max possible width of the image.
                //
                // This is why I'm dividing the height by width.
                //
                // Even if ImageMode isn't Fit, this fixes a layout issue when a user
                // sets the max width but not the height.
                var aspect = (float) Image.Height / (float) Image.Width;
                var height = alottedSize.X * aspect;
                return new Vector2(alottedSize.X, height);
            }
            
            return Image.Bounds.Size;
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            if (Image == null)
                return;
            
            if (ImageMode == ImageMode.Tile)
            {
                for (var x = ContentRectangle.Left; x < ContentRectangle.Right; x += Image.Width)
                {
                    for (var y = ContentRectangle.Top; y < ContentRectangle.Bottom; y += Image.Height)
                    {
                        var rect = new Rectangle(x, y, Image.Width, Image.Height);
                        renderer.FillRectangle(rect, Image, Tint);
                    }
                }   
            }
            else if (ImageMode == ImageMode.Stretch)
            {
                renderer.FillRectangle(ContentRectangle, Image, Tint);
            }
            else if (ImageMode == ImageMode.Fit)
            {
                var aspectRatio = (float) Image.Width / (float) Image.Height;
                var scale = ContentRectangle.Height / (float) Image.Height;

                var scaledHeight = Image.Height * scale;
                var scaledWidth = scaledHeight * aspectRatio;

                var x = ContentRectangle.Left + ((ContentRectangle.Width - scaledWidth) / 2);
                var y = ContentRectangle.Top + ((ContentRectangle.Height - scaledHeight) / 2);

                var rect = new Rectangle(x, y, scaledWidth, scaledHeight);

                renderer.FillRectangle(rect, Image, Tint);
            }
        }
    }

    public enum ImageMode
    {
        Fit,
        Stretch,
        Tile
    }
}