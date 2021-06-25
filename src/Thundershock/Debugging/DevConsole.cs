using System;
using Thundershock.Core;
using Thundershock.Core.Debugging;
using Thundershock.Core.Input;
using Thundershock.Gui;
using Thundershock.Gui.Elements;
using Thundershock.Gui.Elements.Console;

namespace Thundershock.Debugging
{
    public sealed class DevConsole : Layer, ILogOutput
    {
        private GuiSystem _gui;

        private ConsoleControl _console = new();
        private TextEntry _commandEntry = new();
        private Stacker _consoleStack = new();
        private Panel _bg = new();
        
        protected override void OnInit()
        {
            _gui = new GuiSystem(App.Window.GraphicsProcessor);

            Logger.GetLogger().AddOutput(this);

            _console.Properties.SetValue(Stacker.FillProperty, StackFill.Fill);
            
            _consoleStack.Children.Add(_console);
            _consoleStack.Children.Add(_commandEntry);
            _bg.Children.Add(_consoleStack);
            _gui.AddToViewport(_bg);

            _gui.SetFocus(_commandEntry);

            _bg.BackColor = new Color(0x22, 0x22, 0x22);

            _bg.VerticalAlignment = VerticalAlignment.Top;

            _commandEntry.ForeColor = ThundershockPlatform.HtmlColor("#80ff80");
            
            _commandEntry.TextCommitted += CommandEntryOnTextCommitted;
        }

        private void CommandEntryOnTextCommitted(object? sender, EventArgs e)
        {
            var text = _commandEntry.Text;
            _commandEntry.Text = string.Empty;

            _console.WriteLine(" >>> " + text + " <<< ");

            App.GetComponent<CheatManager>().ExecuteCommand(text);
        }

        protected override void OnUnload()
        {
            Logger.GetLogger().RemoveOutput(this);
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            _gui.SetViewportSize(App.Window.Width, App.Window.Height);
            
            _bg.FixedHeight = App.Window.Height / 2;

            _gui.Update(gameTime);
        }

        protected override void OnRender(GameTime gameTime)
        {
            _gui.Render(gameTime);
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            return _gui.KeyDown(e);
        }

        public override bool KeyUp(KeyEventArgs e)
        {
            if (e.Key == Keys.BackQuote)
            {
                Remove();
                return true;
            }
            
            return _gui.KeyUp(e);
        }

        public override bool KeyChar(KeyCharEventArgs e)
        {
            return _gui.KeyChar(e);
        }

        public void Log(string message, LogLevel logLevel)
        {
            var color = logLevel switch
            {
                LogLevel.Info => "f",
                LogLevel.Message => "c",
                LogLevel.Warning => "d",
                LogLevel.Error => "c",
                LogLevel.Fatal => "c",
                LogLevel.Trace => "7",
                _ => "f"
            };

            var messageLog = $"#{color}{message}&0";
            _console.WriteLine(messageLog);
        }
    }
}