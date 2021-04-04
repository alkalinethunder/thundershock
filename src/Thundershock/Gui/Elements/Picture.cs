using System.Net.Mime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Gui.Elements
{
    public class Picture : Element
    {
        public Color Tint { get; set; } = Color.White;
        public Texture2D Image { get; set; }
        public SpriteEffects SpriteEffects { get; set; }

        public bool Tile { get; set; }
        
        protected override Vector2 MeasureOverride()
        {
            return Image?.Bounds.Size.ToVector2() ?? Vector2.Zero;
        }

        protected override void OnPaint(GameTime gameTime, GuiRenderer renderer)
        {
            if (Image == null)
                return;
            
            if (Tile)
            {
                for (var x = BoundingBox.Left; x < BoundingBox.Right; x += Image.Width)
                {
                    for (var y = BoundingBox.Top; y < BoundingBox.Bottom; y += Image.Height)
                    {
                        var rect = new Rectangle(x, y, Image.Width, Image.Height);
                        renderer.FillRectangle(rect, Image, Tint, SpriteEffects);
                    }
                }   
            }
            else
            {
                renderer.FillRectangle(BoundingBox, Image, Tint, SpriteEffects);
            }
        }
    }
}