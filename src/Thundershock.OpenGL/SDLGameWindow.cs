using System;
using SDL2;
using Thundershock.Core;
using Thundershock.Core.Debugging;
using Thundershock.Debugging;

namespace Thundershock.OpenGL
{
    public sealed class SDLGameWindow : GameWindow
    {
        private IntPtr _sdlWindow;
        private SDL.SDL_Event _event;
        
        protected override void OnUpdate()
        {
            PollEvents();
        }

        protected override void Initialize()
        {
            App.Logger.Log("Initializing SDL2...");
            var errno = SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            if (errno != 0)
            {
                App.Logger.Log("SDL initialization HAS FAILED.", LogLevel.Fatal);
                var errText = SDL.SDL_GetError();

                throw new Exception(errText);
            }
            
            App.Logger.Log("Creating an SDL Window...");
            _sdlWindow = SDL.SDL_CreateWindow(this.Title, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, 640, 480,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL);
            App.Logger.Log("SDL window is up. (640x480, SDL_WINDOW_SHOWN | SDL_WINDOW_OPENGL)");
        }

        private void PollEvents()
        {
            while (SDL.SDL_PollEvent(out _event) != 0)
            {
                HandleSdlEvent();
            }
        }

        protected override void OnClosed()
        {
            App.Logger.Log("Destroying the SDL window...");
            SDL.SDL_DestroyWindow(_sdlWindow);
            App.Logger.Log("...done.");
        }

        private void HandleSdlEvent()
        {
            if (_event.type == SDL.SDL_EventType.SDL_QUIT)
            {
                App.Logger.Log("SDL just told us to quit... Letting thundershock know about that.");
                if (!App.Exit())
                {
                    App.Logger.Log("Thundershock app cancelled the exit request.");
                }
            }
        }
    }
}