using System;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock.Components
{
    public class TextComponent : SceneComponent
    {
        private Font _font;

        public Font Font
        {
            get => _font;
            set
            {
                if (_font != value)
                {
                    _font = value ?? throw new ArgumentNullException(nameof(value));
                }
            }
        }
        public string Text { get; set; } = "Text Component";
        public Vector2 Position { get; set; }
        public Color Color { get; set; } = Color.White;
        public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector2 Pivpt { get; set; } = new Vector2(0.5f, 0.5f);

        public Vector2 TextMeasure => _font.MeasureString(Text);
        
        protected override void OnLoad()
        {
            _font = Font.GetDefaultFont(Scene.Graphics);
            base.OnLoad();
        }

        protected override void OnDraw(GameTime gameTime, Renderer2D batch)
        {
            if (!string.IsNullOrWhiteSpace(Text))
            {
                var rect = Scene.ViewportBounds;
                var size = rect.Size;
                var location = rect.Location;

                var origin = location + (size * Origin);
                var pivot = TextMeasure * Pivpt;

                var pos = origin - pivot + Position;

                batch.Begin();
                batch.DrawString(_font, Text, pos, Color);
                batch.End();
            }

            base.OnDraw(gameTime, batch);
        }
    }
}