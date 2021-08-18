using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;

namespace Thundershock.Debugging
{
    internal sealed class TempCommandLine : Layer
    {
        private Renderer2D _renderer;
        private Font _font;
        private CheatManager _cheater;

        private int _cursorPos;
        private string _text = string.Empty;

        protected override void OnInit()
        {
            _font = Font.GetDefaultFont(GamePlatform.GraphicsProcessor);
            _font.Size = 16;

            _renderer = new Renderer2D(GamePlatform.GraphicsProcessor);

            _cheater = App.GetComponent<CheatManager>();
        }

        protected override void OnUnload()
        { 
            
        }

        protected override void OnUpdate(GameTime gameTime)
        {
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Keys.Left:
                    if (_cursorPos > 0)
                        _cursorPos--;
                    break;
                case Keys.Right:
                    if (_cursorPos < _text.Length)
                        _cursorPos++;
                    break;
                case Keys.Home:
                    _cursorPos = 0;
                    break;
                case Keys.End:
                    _cursorPos = _text.Length;
                    break;
                case Keys.Backspace:
                    if (_cursorPos > 0)
                    {
                        _cursorPos--;
                        _text = _text.Remove(_cursorPos, 1);
                    }
                    break;
                case Keys.Enter:
                    if (!string.IsNullOrWhiteSpace(_text))
                    {
                        _cheater.ExecuteCommand(_text);
                    }

                    _text = string.Empty;
                    _cursorPos = 0;
                    Remove();
                    break;
                case Keys.Tab:
                    Remove();
                    break;
            }

            return true;
        }

        public override bool KeyChar(KeyCharEventArgs e)
        {
            switch (e.Key)
            {
                default:
                    _text = _text.Insert(_cursorPos, e.Character.ToString());
                    _cursorPos++;
                    break;
            }
            
            return true;
        }

        protected override void OnRender(GameTime gameTime)
        {
            // TODO: word-wrap
            var text = _text;

            var measure = _font.MeasureString(text);

            var rect = new Rectangle(0, App.Window.Height - measure.Y, App.Window.Width, measure.Y);

            _renderer.ProjectionMatrix =
                Matrix4x4.CreateOrthographicOffCenter(0, App.Window.Width, App.Window.Height, 0, -1, 1);
            
            _renderer.Begin();
            _renderer.FillRectangle(rect, Color.Gray);
            _renderer.DrawString(_font, text, rect.Location, Color.White);
            _renderer.End();
        }

        public override bool KeyUp(KeyEventArgs e)
        {
            return true;
        }
    }
}