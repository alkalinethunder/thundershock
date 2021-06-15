using Thundershock.Core;

namespace Thundershock
{
    public abstract class GraphicalAppBase : AppBase
    {
        private GameWindow _gameWindow;
        private bool _aboutToExit = false;
        
        protected sealed override void Bootstrap()
        {
            Logger.Log("Creating the game window...");
            _gameWindow = CreateGameWindow();
            _gameWindow.Show(this);
            Logger.Log("Game window created.");

            RunLoop();

            Logger.Log("RunLoop just returned. That means we're about to die.");
            _gameWindow.Close();
            _gameWindow = null;
            Logger.Log("Game window destroyed.");
        }

        private void RunLoop()
        {
            while (!_aboutToExit)
            {
                _gameWindow.Update();
            }
        }

        protected override void BeforeExit(AppExitEventArgs args)
        {
            // call the base method to dispatch the event to the rest of the engine.
            base.BeforeExit(args);
            
            // Terminate the game loop if args.Cancelled isn't set
            if (!args.CancelExit)
            {
                _aboutToExit = true;
            }
        }

        protected abstract GameWindow CreateGameWindow();
    }
}