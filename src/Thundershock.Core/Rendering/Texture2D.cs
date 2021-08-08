using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Thundershock.Core.Rendering
{
    public sealed class Texture2D : Texture
    {
        public Texture2D(GraphicsProcessor gpu, int width, int height) : base(gpu, width, height)
        {
        }

        public static Texture2D FromResource(GraphicsProcessor gpu, Assembly ass, string resource)
        {
            if (Resource.GetStream(ass, resource, out var stream))
            {
                return FromStream(gpu, stream);
            }
            else
            {
                throw new InvalidOperationException("Resource not found.");
            }
        }

        public static Texture2D FromFile(GraphicsProcessor gpu, string path)
        {
            using var file = File.OpenRead(path);
            return FromStream(gpu, file);
        }
        
        public static Texture2D FromStream(GraphicsProcessor gpu, Stream stream)
        {
            // load the stream as a bitmap using System.Drawing
            var bmp = (Bitmap) Image.FromStream(stream);
            
            // We now know the width and height.
            var width = bmp.Width;
            var height = bmp.Height;
            
            // Now we can copy the bitmap data from the bitmap into an array that we'll eventually
            // upload to the graphics card.
            //
            // We start by locking the bitmap data in memory and allocating space for us to pull the data
            // into the land of managed code.
            var bmpData = Array.Empty<byte>();
            var rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            var lck = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            
            // Now that the data is locked in place, we can allocate space for it.
            Array.Resize(ref bmpData, Math.Abs(lck.Stride) * lck.Height);
            
            // Now we can copy the data from native land into managed land.
            Marshal.Copy(lck.Scan0, bmpData, 0, bmpData.Length);
            
            // Now we can unlock the bitmap and then free it, we're done with it.
            bmp.UnlockBits(lck);
            bmp.Dispose();

            // GDI+ gives us the bytes in the order of BGRA, but the Thundershock graphics pipeline works in
            // RGBA. So we need to swap the bytes.
            for (var i = 0; i < bmpData.Length; i += 4)
            {
                var b = bmpData[i];
                var r = bmpData[i + 2];
                
                bmpData[i] = r;
                bmpData[i + 2] = b;
            }
            
            // Create the texture.
            var texture = new Texture2D(gpu, width, height);
            
            // Upload the pixel data to the texture.
            texture.Upload(new ReadOnlySpan<byte>(bmpData, 0, bmpData.Length));
            
            // Return the texture!
            return texture;
        }

        public static Texture2D FromPak(GraphicsProcessor gpu, Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            var width = reader.ReadInt32();
            var height = reader.ReadInt32();

            var depth = reader.ReadByte();
            
            var pixels = reader.ReadBytes((width * depth) * height);

            reader.Close();

            var tex = new Texture2D(gpu, width, height);
            tex.Upload(pixels);
            return tex;
        }
    }
}