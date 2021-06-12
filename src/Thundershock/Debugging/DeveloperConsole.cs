using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Thundershock.Gui;
using Thundershock.Gui.Elements;
using Thundershock.Gui.Elements.Console;
using Thundershock.Input;

namespace Thundershock.Debugging
{
    public class DeveloperConsole : SceneComponent, ILogOutput
    {
        private GuiSystem _devGui;
        private CheatManager _cheatManager;
        private InputManager _input;
        
        // dev UI
        private Panel _backdrop = new();
        private ConsoleControl _console = new();
        private TextBlock _title = new();
        private Stacker _stacker = new();
        
        protected override void OnLoad()
        {
            // Add us as a log output.
            App.Logger.AddOutput(this);
            
            // Input system
            _input = App.GetComponent<InputManager>();
            _cheatManager = App.GetComponent<CheatManager>();
            
            // add the GUI system to the scene.
            _devGui = Scene.AddComponent<GuiSystem>();

            // Set up the layout.
            _backdrop.Children.Add(_stacker);
            _stacker.Children.Add(_title);
            _stacker.Children.Add(_console);
            _devGui.AddToViewport(_backdrop);
            _console.Properties.SetValue(Stacker.FillProperty,StackFill.Fill);
            _backdrop.FixedHeight = 460;
            _backdrop.VerticalAlignment = VerticalAlignment.Top;

            // Style it.
            _backdrop.BackColor = Color.White * 0.5f;
            _stacker.Padding = 15;
            _title.Text = "Developer console";
            _backdrop.Visibility = Visibility.Collapsed;
            
            // Bind to keyboard input for opening the console.
            _input.KeyDown += InputOnKeyDown;
            
            // Bind to the console losing focus. This closes it.
            _console.Blurred += ConsoleOnBlurred;
            
            base.OnLoad();
        }

        private void ConsoleOnBlurred(object? sender, FocusChangedEventArgs e)
        {
            _backdrop.Visibility = Visibility.Collapsed;
        }

        protected override void OnUnload()
        {
            Scene.RemoveComponent(_devGui);
            _input.KeyDown -= InputOnKeyDown;
            App.Logger.RemoveOutput(this);
            base.OnUnload();
        }

        private void InputOnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Keys.F3)
            {
                if (_backdrop.Visibility == Visibility.Visible)
                {
                    _devGui.SetFocus(null);
                }
                else
                {
                    _backdrop.Visibility = Visibility.Visible;
                    _devGui.SetFocus(_console);
                }
            }
        }

        public void Log(string message, LogLevel logLevel)
        {
            var colorCode = logLevel switch
            {
                LogLevel.Info => "#7",
                LogLevel.Message => "#a",
                LogLevel.Warning => "#6",
                LogLevel.Error => "#c",
                LogLevel.Fatal => "#4",
                LogLevel.Trace => "#8",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
            };

            var textIndex = message.IndexOf("(): ") + "(): ".Length;

            var consoleMessage = $"{colorCode}{message.Substring(0, textIndex)}&w{message.Substring(textIndex)}&W&0";

            _console.WriteLine(consoleMessage);
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            if (_console.GetLine(out var cheat))
            {
                if (!string.IsNullOrWhiteSpace(cheat))
                {
                    _console.WriteLine($"&b#5 >>> {cheat} <<<&B&0");
                    _console.WriteLine();
                    _cheatManager.ExecuteCommand(cheat);
                }
            }
            
            base.OnUpdate(gameTime);
        }
    }
}