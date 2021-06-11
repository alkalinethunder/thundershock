using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Thundershock.Gui.Elements;
using Thundershock.Rendering;
using Color = Microsoft.Xna.Framework.Color;

namespace Thundershock.Debugging
{
    public class WarningPrinter : SceneComponent
    {
        private double _cooldown;
        private class DebugTextItem
        {
            public double TimeLeft;
            public Color Color;
            public string Text;
        }

        private class WarningPrinterLogOutput : ILogOutput
        {
            public void Log(string message, LogLevel logLevel)
            {
                var color = logLevel switch
                {
                    LogLevel.Info => Color.Transparent,
                    LogLevel.Message => Color.Green,
                    LogLevel.Warning => Color.Orange,
                    LogLevel.Error => Color.Red,
                    LogLevel.Fatal => Color.DarkRed,
                    LogLevel.Trace => Color.Transparent
                };

                if (color == Color.Transparent)
                    return;

                var textItem = new DebugTextItem
                {
                    TimeLeft = 5,
                    Color = color,
                    Text = message
                };

                _textItems.Insert(0, textItem);
            }
        }
        
        private static List<DebugTextItem> _textItems = new();
        private SpriteFont _font;

        internal static ILogOutput GetWarningPrinterLogOutput()
        {
            return new WarningPrinterLogOutput();
        }
        
        protected override void OnLoad()
        {
            // Load the debug font.
            _font = App.EngineContent.Load<SpriteFont>("Fonts/DebugSmall");
            
            base.OnLoad();
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            if (_cooldown > 0)
            {
                _cooldown -= gameTime.ElapsedGameTime.TotalSeconds;
                return;
            }
            
            for (var i = 0; i < _textItems.Count; i++)
            {
                var item = _textItems[i];
                if (item.TimeLeft <= 0)
                {
                    _textItems.RemoveAt(i);
                    _cooldown = 0.1;
                }
                else
                {
                    item.TimeLeft -= gameTime.ElapsedGameTime.TotalSeconds;
                }
            }
            
            base.OnUpdate(gameTime);
        }

        protected override void OnDraw(GameTime gameTime, Renderer renderer)
        {
            var rect = this.Scene.ViewportBounds;
            rect.X += 15;
            rect.Y += 15;
            rect.Width -= 30;
            rect.Height -= 30;

            var padY = 5;
            var padX = 5;
            var y = rect.Top;
            var x = rect.Left;
            
            renderer.Begin();
            foreach (var item in _textItems)
            {
                var maxTextWidth = rect.Width - (padX * 2);
                var text = TextBlock.WordWrap(_font, item.Text, maxTextWidth);

                foreach (var line in text.Split('\n'))
                {
                    var measure = _font.MeasureString(line);

                    var bgRect = new Rectangle(x, y, (int) measure.X + (padX * 2), (int) measure.Y + (padY * 2));
                    
                    renderer.FillRectangle(bgRect, Color.Black * 0.8f);

                    var textPos = new Vector2(x + padX, y + padY);

                    renderer.DrawString(_font, line, textPos + new Vector2(2, 2), Color.Black);
                    renderer.DrawString(_font, line, textPos, item.Color);

                    y += bgRect.Height;
                }
            }

            renderer.End();
        }
    }
}