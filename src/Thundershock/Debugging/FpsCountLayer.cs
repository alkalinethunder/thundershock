using System;
using System.Globalization;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock.Debugging
{
    public sealed class FpsCountLayer : Layer
    {
        private Renderer2D _renderer;
        private Font _font;
        private string _fps;
        
        protected override void OnInit()
        {
            _renderer = new Renderer2D(GamePlatform.GraphicsProcessor);
            _font = Font.GetDefaultFont(GamePlatform.GraphicsProcessor);
        }

        protected override void OnUnload()
        {
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            _fps = Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        protected override void OnRender(GameTime gameTime)
        {
            var transform = Matrix4x4.CreateOrthographicOffCenter(0, App.Window.Width, App.Window.Height, 0, -1, 1);

            _renderer.ProjectionMatrix = transform;

            _renderer.Begin();
            _renderer.DrawString(_font, _fps, Vector2.Zero, Color.White);
            _renderer.End();
        }
    }
}