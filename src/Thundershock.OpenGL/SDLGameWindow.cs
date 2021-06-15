using System;
using OpenGL;
using SDL2;
using Thundershock.Core;
using Thundershock.Core.Debugging;
using Thundershock.Core.Rendering;
using Thundershock.Debugging;

namespace Thundershock.OpenGL
{
    public sealed class SDLGameWindow : GameWindow
    {
        private IntPtr _sdlWindow;
        private IntPtr _glContext;
        private SDL.SDL_Event _event;
        private GLRenderer _renderer;

        public override Renderer Renderer => _renderer;
        
        protected override void OnUpdate()
        {
            PollEvents();
            
            // Swap the OpenGL buffers so we can see what was just rendered by
            // Thundershock.
            SDL.SDL_GL_SwapWindow(_sdlWindow);
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

            SetupGLRenderer();
        }

        private void SetupGLRenderer()
        {
            App.Logger.Log("Setting up the SDL OpenGL renderer...");
            var ctx = SDL.SDL_GL_CreateContext(_sdlWindow);
            if (ctx == IntPtr.Zero)
            {
                var err = SDL.SDL_GetError();
                App.Logger.Log(err, LogLevel.Error);
                throw new Exception(err);
            }

            _glContext = ctx;
            App.Logger.Log("GL Context created.");

            // Make the newly created context the current one.
            SDL.SDL_GL_MakeCurrent(_sdlWindow, _glContext);

            // Glue OpenGL and SDL2 together.
            GL.Import(SDL.SDL_GL_GetProcAddress);
            
            _renderer = new GLRenderer();
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