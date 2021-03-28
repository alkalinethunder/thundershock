using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Components
{
    public class TextComponent : SceneComponent
    {
        private SpriteFont _font;

        public SpriteFont Font
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
            _font = Game.Content.Load<SpriteFont>("Fonts/DebugSmall");
            base.OnLoad();
        }

        protected override void OnDraw(GameTime gameTime, SpriteBatch batch)
        {
            if (!string.IsNullOrWhiteSpace(Text))
            {
                var rect = new Rectangle(0, 0, Game.ScreenWidth, Game.ScreenHeight);
                var size = rect.Size.ToVector2();
                var location = rect.Location.ToVector2();

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