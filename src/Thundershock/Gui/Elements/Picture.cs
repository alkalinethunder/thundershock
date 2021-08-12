using System;
using Thundershock.Core;
using Thundershock.Core.Rendering;
using System.Numerics;

namespace Thundershock.Gui.Elements
{
    public class Picture : ContentElement
    {
        private Texture2D _image;
        
        public Color Tint { get; set; } = Color.White;

        public Color BorderColor { get; set; } = Color.White;

        public HorizontalContentAlignment ImageAlignment { get; set; }
        
        public int BorderWidth { get; set; } = 0;
        
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
            {
                if (ImageMode == ImageMode.Rounded)
                {
                    var radius = (Math.Min(ContentRectangle.Width, ContentRectangle.Height) / 2);
                    renderer.FillCircle(ContentRectangle.Center, radius, Tint);
                }
                else
                {
                    renderer.FillRectangle(ContentRectangle, Tint);
                }

                return;
            }

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

                if (BorderWidth > 0)
                {
                    renderer.DrawRectangle(ContentRectangle, BorderColor, BorderWidth);
                }
            }
            else if (ImageMode == ImageMode.Stretch)
            {
                renderer.FillRectangle(ContentRectangle, Image, Tint);
                
                if (BorderWidth > 0)
                {
                    renderer.DrawRectangle(ContentRectangle, BorderColor, BorderWidth);
                }
            }
            else if (ImageMode == ImageMode.Fit)
            {
                var aspectRatio = (float) Image.Width / (float) Image.Height;
                var scale = ContentRectangle.Height / (float) Image.Height;

                var scaledHeight = Image.Height * scale;
                var scaledWidth = scaledHeight * aspectRatio;

                var x = GetImageLocX(scaledWidth);
                var y = ContentRectangle.Top + ((ContentRectangle.Height - scaledHeight) / 2);

                var rect = new Rectangle(x, y, scaledWidth, scaledHeight);

                renderer.FillRectangle(rect, Image, Tint);
                
                if (BorderWidth > 0)
                {
                    renderer.DrawRectangle(rect, BorderColor, BorderWidth);
                }
            }
            else if (ImageMode == ImageMode.Zoom)
            {
                var aspectRatio = (float) Image.Height / (float) Image.Width;


                var scaledHeight = ContentRectangle.Width * aspectRatio;
                var scaledWidth = ContentRectangle.Width;

                var x = ContentRectangle.Left;
                var y = ContentRectangle.Top + ((ContentRectangle.Height - scaledHeight) / 2);

                var rect = new Rectangle(x, y, scaledWidth, scaledHeight);

                renderer.FillRectangle(rect, Image, Tint);
                
                if (BorderWidth > 0)
                {
                    renderer.DrawRectangle(rect, BorderColor, BorderWidth);
                }

            }
            else if (ImageMode == ImageMode.Rounded)
            {
                // Rounded images are ALWAYS square. I know that sounds drunk, but what I mean is,
                // the image size will be considered to be uniform even if it isn't.
                var diameter = (float) Math.Min(Image.Width, Image.Height);

                // Scale based on the smallest dimension of the UI element.
                var scale = Math.Min(ContentRectangle.Width, ContentRectangle.Height) / diameter;
                
                // Get the scaled image dimensions.
                var scaledWidth = diameter * scale;
                var scaledHeight = diameter * scale;
                
                // Calculate the top-left coordinate of the image rect.
                var x = ContentRectangle.Left + ((ContentRectangle.Width - scaledWidth) / 2);
                var y = ContentRectangle.Top + ((ContentRectangle.Height - scaledHeight) / 2);

                // Create the rect.
                var rect = new Rectangle(x, y, scaledWidth, scaledHeight);
                
                // Center-point and radius is needed for circles in Thundershock 2D.
                var radius = scaledWidth / 2;
                var center = rect.Center;

                if (BorderWidth > 0)
                {
                    renderer.FillCircle(center, radius, BorderColor);
                    radius -= BorderWidth;
                }
                
                // Render the circle.
                renderer.FillCircle(center, radius, Image, Tint);
            }
        }

        private float GetImageLocX(float imageWidth)
        {
            return ImageAlignment switch
            {
                HorizontalContentAlignment.Left => ContentRectangle.Left,
                HorizontalContentAlignment.Center => ContentRectangle.Left +
                                                     ((ContentRectangle.Width - imageWidth) / 2),
                HorizontalContentAlignment.Right => ContentRectangle.Right - imageWidth,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public enum ImageMode
    {
        Fit,
        Stretch,
        Tile,
        Zoom,
        Rounded
    }

    public enum VerticalContentAlignment
    {
        Top,
        Center,
        Bottom
    }

    public enum HorizontalContentAlignment
    {
        Left,
        Center,
        Right
    }
}