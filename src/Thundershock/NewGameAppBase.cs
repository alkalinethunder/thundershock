using Thundershock.Core;
using Thundershock.OpenGL;

namespace Thundershock
{
    public class NewGameAppBase : GraphicalAppBase
    {
        protected override GameWindow CreateGameWindow()
        {
            return new SDLGameWindow();
        }
    }
}