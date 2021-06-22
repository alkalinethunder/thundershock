using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using SDL2;
using Thundershock.Core;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;
using Thundershock.Debugging;
using Thundershock.Core.Audio;

using Silk.NET.OpenGL;

namespace Thundershock.OpenGL
{
    public sealed class SDLGameWindow : GameWindow
    {
        private GL _gl;
        private int _wheelX;
        private int _wheelY;
        private IntPtr _sdlWindow;
        private IntPtr _glContext;
        private SDL.SDL_Event _event;
        private GlGraphicsProcessor _graphicsProcessor;
        private OpenAlAudioBackend _audio;

        public override AudioBackend AudioBackend => _audio;
        public override GraphicsProcessor GraphicsProcessor => _graphicsProcessor;
        
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
            
            CreateSdlWindow();

            App.Logger.Log("Initializing SDL_mixer audio backend...");
            _audio = new OpenAlAudioBackend();
        }

        private void SetupGLRenderer()
        {
            // Set up the OpenGL context attributes.
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 5);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK,
                SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
            
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
            _gl = Silk.NET.OpenGL.GL.GetApi(SDL.SDL_GL_GetProcAddress);
            _gl.Enable(EnableCap.DebugOutput);
            _gl.DebugMessageCallback(PrintGLError, 0);
            _graphicsProcessor = new GlGraphicsProcessor(_gl);
            
            // Set the viewport size.
            _graphicsProcessor.SetViewportArea(0, 0, Width, Height);
            
            // Disable V-Sync for testing renderer optimizations.
            // TODO: Allow the engine to do this.
            SDL.SDL_GL_SetSwapInterval(0);
            
            // Initialize the platform layer now that we have GL
            GamePlatform.Initialize(new SDLGamePlatform(_gl));
        }

        private void PrintGLError(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userparam)
        {
            var buf = new byte[length];
            Marshal.Copy(message, buf, 0, buf.Length);

            var messageString = Encoding.UTF8.GetString(buf);

            var logLevel = severity switch
            {
                GLEnum.DebugSeverityLow => LogLevel.Warning,
                GLEnum.DebugSeverityMedium => LogLevel.Error,
                GLEnum.DebugSeverityHigh => LogLevel.Fatal,
                _ => LogLevel.Trace
            };

            if (logLevel != LogLevel.Trace)
                App.Logger.Log(messageString, logLevel);
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
            DestroySdlWindow();
            App.Logger.Log("...done.");
        }

        private void HandleSdlEvent()
        {
            if (_event.type == SDL.SDL_EventType.SDL_WINDOWEVENT)
            {
                if (_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                {
                    ReportClientSize(_event.window.data1, _event.window.data2);
                }
            }
            
            if (_event.type == SDL.SDL_EventType.SDL_QUIT)
            {
                App.Logger.Log("SDL just told us to quit... Letting thundershock know about that.");
                if (!App.Exit())
                {
                    App.Logger.Log("Thundershock app cancelled the exit request.");
                }
            }

            if (_event.type == SDL.SDL_EventType.SDL_KEYDOWN || _event.type == SDL.SDL_EventType.SDL_KEYUP)
            {
                var key = (Keys) _event.key.keysym.sym;
                var repeat = _event.key.repeat != 0;
                var isPressed = _event.key.state == SDL.SDL_PRESSED;

                // Dispatch the event to thundershock.
                DispatchKeyEvent(key, '\0', isPressed, repeat, false);

                // Dispatch a keychar event for the \r (carriage return) key for Enter.
                // Higher-level areas of the engine use this for text editing, specifically
                // ConsoleControl and a lot of things in RED TEAM.
                if (key == Keys.Enter)
                {
                    DispatchKeyEvent(key, '\r', isPressed, repeat, true);
                }
            }

            if (_event.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN ||
                _event.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP)
            {
                var button = MapSdlMouseButton(_event.button.button);
                var state = _event.button.state == SDL.SDL_PRESSED ? ButtonState.Pressed : ButtonState.Released;

                DispatchMouseButton(button, state);
            }

            if (_event.type == SDL.SDL_EventType.SDL_MOUSEWHEEL)
            {
                var xDelta = _event.wheel.x;
                var yDelta = _event.wheel.y;
                
                if (_event.wheel.direction == (uint) SDL.SDL_MouseWheelDirection.SDL_MOUSEWHEEL_FLIPPED)
                {
                    xDelta = xDelta * -1;
                    yDelta = yDelta * -1;
                }

                if (yDelta != 0)
                {
                    _wheelY += yDelta;
                    ReportMouseScroll(_wheelY, yDelta, ScrollDirection.Vertical);
                }

                if (xDelta != 0)
                {
                    _wheelX += xDelta;
                    ReportMouseScroll(_wheelX, xDelta, ScrollDirection.Horizontal);
                }
            }
            
            if (_event.type == SDL.SDL_EventType.SDL_TEXTINPUT)
            {
                var text = string.Empty;

                unsafe
                {
                    var count = SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE;
                    var end = 0;
                    while (end < count && _event.text.text[end] > 0)
                        end++;

                    fixed (byte* bytes = _event.text.text)
                    {
                        var span = new ReadOnlySpan<byte>(bytes, end);
                        text = Encoding.UTF8.GetString(span);
                    }
                }
                
                foreach (var character in text)
                {
                    var key = (Keys) character;
                    DispatchKeyEvent(key, character, false, false, true);
                }
            }

            if (_event.type == SDL.SDL_EventType.SDL_MOUSEMOTION)
            {
                ReportMousePosition(_event.motion.x, _event.motion.y);
            }
        }

        private MouseButton MapSdlMouseButton(uint button)
        {
            return button switch
            {
                SDL.SDL_BUTTON_LEFT => MouseButton.Primary,
                SDL.SDL_BUTTON_RIGHT => MouseButton.Secondary,
                SDL.SDL_BUTTON_MIDDLE => MouseButton.Middle,
                SDL.SDL_BUTTON_X1 => MouseButton.BrowserForward,
                SDL.SDL_BUTTON_X2 => MouseButton.BrowserBack,
                _ => throw new NotSupportedException()
            };
        }
        
        protected override void OnWindowTitleChanged()
        {
            SDL.SDL_SetWindowTitle(_sdlWindow, Title);
        }

        private uint GetWindowModeFlags()
        {
            var flags = 0x00u;

            if (IsBorderless)
            {
                flags |= (uint) SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
            }

            if (IsFullScreen)
            {
                if (IsBorderless)
                    flags |= (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP;
                else
                    flags |= (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
            }

            return flags;
        }

        protected override void OnWindowModeChanged()
        {
            // DestroySdlWindow();
            // CreateSdlWindow();

            var fsFlags = 0u;
            if (IsBorderless && IsFullScreen)
                fsFlags |= (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
            else if (IsFullScreen)
                fsFlags |= (uint) SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP;
            
            SDL.SDL_SetWindowBordered(_sdlWindow, IsBorderless ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE);
            SDL.SDL_SetWindowFullscreen(_sdlWindow, fsFlags);

            if (fsFlags > 0)
            {
                SDL.SDL_SetWindowPosition(_sdlWindow, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED);
            }
            
            base.OnWindowModeChanged();
        }

        protected override void OnClientSizeChanged()
        {
            // Resize the SDL window.
            SDL.SDL_SetWindowSize(_sdlWindow, Width, Height);
            SDL.SDL_SetWindowPosition(_sdlWindow, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED);

            ReportClientSize(Width, Height);
            base.OnClientSizeChanged();
        }

        private void CreateSdlWindow()
        {
            var flags = GetWindowModeFlags();
            flags |= (uint) SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL;
            flags |= (uint) SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN;
            
            App.Logger.Log("Creating an SDL Window...");
            _sdlWindow = SDL.SDL_CreateWindow(this.Title, 0, 0, Width, Height,
                (SDL.SDL_WindowFlags) flags);
            App.Logger.Log("SDL window is up. (640x480, SDL_WINDOW_SHOWN | SDL_WINDOW_OPENGL)");

            SetupGLRenderer();
        }

        private void DestroySdlWindow()
        {
            App.Logger.Log("Destroying current GL renderer...");
            SDL.SDL_GL_DeleteContext(_glContext);
            _glContext = IntPtr.Zero;
            _graphicsProcessor = null;
            
            App.Logger.Log("Destroying the SDL window...");
            SDL.SDL_DestroyWindow(_sdlWindow);
        }
    }
}