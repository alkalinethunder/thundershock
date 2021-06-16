using System;
using SDL2;
using Thundershock.Core;

namespace Thundershock.OpenGL
{
    public sealed class SDLGamePlatform : IGamePlatform
    {
        public int GetMonitorCount()
        {
            return SDL.SDL_GetNumVideoDisplays();
        }

        public DisplayMode GetDefaultDisplayMode(int monitor)
        {
            var bounds = new SDL.SDL_Rect();
            var result = SDL.SDL_GetDisplayBounds(monitor, out bounds);

            if (result != 0)
                throw new Exception(SDL.SDL_GetError());

            return new DisplayMode(bounds.w, bounds.h, monitor, bounds.x, bounds.y);
        }
    }
}