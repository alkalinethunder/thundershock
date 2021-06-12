using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Thundershock.Gui.Elements.Console
{
    public class ColorPalette
    {
        private Dictionary<ConsoleColor, Color> _map = new Dictionary<ConsoleColor, Color>();

        public Color PanicWindowColor { get; set; }
        public Color DefaultWindowColor { get; set; }
        
        public Color CompletionsBackground { get; set; }
        public Color CursorColor { get; set; }
        public Color CursorForeground { get; set; }
        public Color CompletionsHighlight { get; set; }
        public Color CompletionsHighlightText { get; set; }
        public Color CompletionsText { get; set; }

        public bool PanicMode { get; set; }
        
        public Texture2D BackgroundImage { get; set; }
        
        public ColorPalette()
        {
            _map.Add(ConsoleColor.Black, Color.Black);
            _map.Add(ConsoleColor.White, Color.White);
            _map.Add(ConsoleColor.Red, Color.Red);
            _map.Add(ConsoleColor.Green, Color.Green);
            _map.Add(ConsoleColor.Blue, Color.Blue);
            _map.Add(ConsoleColor.Gray, Color.Gray);
            _map.Add(ConsoleColor.DarkGray, Color.DarkGray);
            _map.Add(ConsoleColor.Cyan, Color.Cyan);
            _map.Add(ConsoleColor.Magenta, Color.Magenta);
            _map.Add(ConsoleColor.Yellow, Color.Yellow);
            _map.Add(ConsoleColor.DarkYellow, Color.Orange);
            _map.Add(ConsoleColor.DarkRed, Color.DarkRed);
            _map.Add(ConsoleColor.DarkGreen, Color.DarkGreen);
            _map.Add(ConsoleColor.DarkBlue, Color.DarkBlue);
            _map.Add(ConsoleColor.DarkCyan, Color.DarkCyan);
            _map.Add(ConsoleColor.DarkMagenta, Color.DarkMagenta);
        }
        
        public Color GetColor(ConsoleColor consoleColor)
        {
            var color = _map[consoleColor];

            if (PanicMode)
                color = Color.Lerp(color, Color.Red, 0.224f);
            
            return color;
        }

        public void SetColor(ConsoleColor consoleColor, Color color)
        {
            _map[consoleColor] = color;
        }
    }
}