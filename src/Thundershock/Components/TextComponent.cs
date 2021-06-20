using System;
using System.Numerics;
using Cairo;
using SDL2;
using Thundershock.Core;
using Thundershock.Core.Rendering;
using Color = Thundershock.Core.Color;

namespace Thundershock.Components
{
    public class TextComponent : SceneComponent
    {
        private Font _font;

        public Transform2D Transform { get; } = new Transform2D();

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
        public Color Color { get; set; } = Color.White;
        public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f);

        public Vector2 TextMeasure => _font.MeasureString(Text);
        
        protected override void OnLoad()
        {
            _font = Font.GetDefaultFont(Scene.Graphics);
            base.OnLoad();
        }

        protected override void OnDraw(GameTime gameTime, Renderer2D batch)
        {
            // TODO: This should really be dealt with by a central rendering system...

            var size = _font.MeasureString(Text);
            var pivot = size * Pivot;

            var transform = Transform.GetTransformMatrix();
            var proj = batch.ProjectionMatrix;
            batch.ProjectionMatrix *= transform;
            
            if (!string.IsNullOrWhiteSpace(Text))
            {
                batch.Begin();
                batch.DrawString(_font, Text, -pivot, Color);
                batch.End();
            }

            batch.ProjectionMatrix = proj;
            
            base.OnDraw(gameTime, batch);
        }
    }
}