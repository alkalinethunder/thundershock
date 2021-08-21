using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Svg;
using Thundershock.Core;
using Thundershock.Core.Rendering;
using Thundershock.IO;

namespace Thundershock.GameFramework
{
    public class SvgImage
    {
        private SvgDocument _doc;

        private SvgImage(SvgDocument doc)
        {
            _doc = doc;
        }

        public Texture2D GetTexture(int size)
            => GetTexture(size, size);

        public Texture2D GetTexture(int width, int height)
        {
            // Start by checking to see if we have a graphics processor.
            if (GamePlatform.GraphicsProcessor == null)
                throw new InvalidOperationException("The graphics subsystem is not currently active.");
            
            // Since we know we have a GPU, let's create a texture for the SVG raster.
            var texture = new Texture2D(GamePlatform.GraphicsProcessor, width, height, TextureFilteringMode.Linear);
            
            // Create a GDI bitmap - it's the only way to rasterize the SVG using the current library.
            using var svgRasterImage = new Bitmap(width, height);
            
            // Create a new SVG renderer.
            // We'll use it to rasterize the SVG to the bitmap, we don't need it for anything else.
            // Your IDE will tell you to turn this into a using statement rather than a using block,
            // I will not merge your pull request if you do.
            using (var renderer = SvgRenderer.FromImage(svgRasterImage))
            {
                // Render the SVG to the bitmap.
                _doc.RenderElement(renderer);
            }
            
            // Lock the bitmap so we can pull the pixel data out.
            var bmpLock = svgRasterImage.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            
            // Allocate managed memory for us to pull the pixel data into.
            // Then copy the data in.
            var bytes = new byte[Math.Abs(bmpLock.Stride) * bmpLock.Height];
            Marshal.Copy(bmpLock.Scan0, bytes, 0, bytes.Length);
            
            // Now we need to swap the red and blue channels for each pixel.
            // GDI+ orders pixels in (r, g, b, a) order, thundershock textures
            // expect (b, g, r, a) order.
            for (var i = 0; i < bytes.Length; i += 4)
            {
                var r = bytes[i];
                var b = bytes[i + 2];
                bytes[i] = b;
                bytes[i + 2] = r;
            }
            
            // Done. Unlock the bitmap and destroy it.
            svgRasterImage.UnlockBits(bmpLock);
            svgRasterImage.Dispose();
            
            // Upload the pixel data to the texture.
            texture.Upload(bytes);
            
            // And we're done!
            // POSSIBLE TODO: We could theoretically cache this texture, which would
            //                make things faster - but the textures would need to be
            //                discarded when the GPU changes.
            return texture;
        }

        public static SvgImage FromStream(Stream stream)
        {
            // Open a string stream reader.
            using var reader = new StreamReader(stream);

            // Read the SVG text.
            var svg = reader.ReadToEnd();

            // Parse it.
            var doc = SvgDocument.FromSvg<SvgDocument>(svg);

            // Create the engine abstraction over it.
            var svgImage = new SvgImage(doc);
            
            // Kill the stream reader.
            reader.Close();

            // Return the SvgImage.
            return svgImage;
        }

        public static SvgImage FromFile(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            var img = FromStream(stream);
            stream.Close();
            return img;
        }

        public static SvgImage FromFile(FileSystem fs, string path)
        {
            using var stream = fs.OpenFile(path);
            var img = FromStream(stream);
            stream.Close();
            return img;
        }
    }
}