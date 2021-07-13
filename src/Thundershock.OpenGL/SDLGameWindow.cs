using System;
using System.Runtime.InteropServices;
using System.Text;
using Thundershock.Core;
using Thundershock.Core.Input;
using Thundershock.Core.Rendering;
using Thundershock.Core.Audio;

using Silk.NET.OpenGL;
using Thundershock.Core.Debugging;

namespace Thundershock.OpenGL
{
    public sealed class SdlGameWindow : GameWindow
    {
        private GL _gl;
        private int _wheelX;
        private int _wheelY;
        private IntPtr _sdlWindow;
        private IntPtr _glContext;
        private Sdl.SdlEvent _event;
        private GlGraphicsProcessor _graphicsProcessor;
        private OpenAlAudioBackend _audio;

        public override AudioBackend AudioBackend => _audio;
        public override GraphicsProcessor GraphicsProcessor => _graphicsProcessor;
        
        protected override void OnUpdate()
        {
            PollEvents();

            // Swap the OpenGL buffers so we can see what was just rendered by
            // Thundershock.
            Sdl.SDL_GL_SwapWindow(_sdlWindow);
        }

        protected override void Initialize()
        {
            App.Logger.Log("Initializing SDL2...");
            var errno = Sdl.SDL_Init(Sdl.SdlInitVideo);
            if (errno != 0)
            {
                App.Logger.Log("SDL initialization HAS FAILED.", LogLevel.Fatal);
                var errText = Sdl.SDL_GetError();

                throw new Exception(errText);
            }

            App.Logger.Log("Initializing SDL_mixer audio backend...");
            _audio = new OpenAlAudioBackend();

            CreateSdlWindow();
        }

        private void SetupGlRenderer()
        {
            // Set up the OpenGL context attributes.
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlContextMajorVersion, 4);
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlContextMinorVersion, 5);
            Sdl.SDL_GL_SetAttribute(Sdl.SdlGLattr.SdlGlContextProfileMask,
                Sdl.SdlGLprofile.SdlGlContextProfileCore);
            
            App.Logger.Log("Setting up the SDL OpenGL renderer...");
            var ctx = Sdl.SDL_GL_CreateContext(_sdlWindow);
            if (ctx == IntPtr.Zero)
            {
                var err = Sdl.SDL_GetError();
                App.Logger.Log(err, LogLevel.Error);
                throw new Exception(err);
            }

            _glContext = ctx;
            App.Logger.Log("GL Context created.");

            // Make the newly created context the current one.
            Sdl.SDL_GL_MakeCurrent(_sdlWindow, _glContext);

            // Glue OpenGL and SDL2 together.
            _gl = GL.GetApi(Sdl.SDL_GL_GetProcAddress);
#if DEBUG
            _gl.Enable(EnableCap.DebugOutput);
            _gl.DebugMessageCallback(PrintGlError, 0);
#endif
            _graphicsProcessor = new GlGraphicsProcessor(_gl);
            
            // Set the viewport size.
            _graphicsProcessor.SetViewportArea(0, 0, Width, Height);
            
            // Disable V-Sync for testing renderer optimizations.
            // TODO: Allow the engine to do this.
            Sdl.SDL_GL_SetSwapInterval(0);
            
            // Initialize the platform layer now that we have GL
            GamePlatform.Initialize(new SdlGamePlatform(_gl, _audio));
        }

#if DEBUG
        private void PrintGlError(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userparam)
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
#endif
        
        private void PollEvents()
        {
            while (Sdl.SDL_PollEvent(out _event) != 0)
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
            switch (_event.type)
            {
                case Sdl.SdlEventType.SdlWindowevent:
                    if (_event.window.windowEvent == Sdl.SdlWindowEventId.SdlWindoweventResized)
                    {
                        ReportClientSize(_event.window.data1, _event.window.data2);
                    }

                    break;
                case Sdl.SdlEventType.SdlQuit:

                    App.Logger.Log("SDL just told us to quit... Letting thundershock know about that.");
                    if (!App.Exit())
                    {
                        App.Logger.Log("Thundershock app cancelled the exit request.");
                    }

                    break;
                case Sdl.SdlEventType.SdlKeydown:
                case Sdl.SdlEventType.SdlKeyup:
                    var key = (Keys) _event.key.keysym.sym;
                    var repeat = _event.key.repeat != 0;
                    var isPressed = _event.key.state == Sdl.SdlPressed;

                    // Dispatch the event to thundershock.
                    DispatchKeyEvent(key, '\0', isPressed, repeat, false);
                    break;
                case Sdl.SdlEventType.SdlMousebuttondown:
                case Sdl.SdlEventType.SdlMousebuttonup:

                    var button = MapSdlMouseButton(_event.button.button);
                    var state = _event.button.state == Sdl.SdlPressed ? ButtonState.Pressed : ButtonState.Released;

                    DispatchMouseButton(button, state);
                    break;

                case Sdl.SdlEventType.SdlMousewheel:

                    var xDelta = _event.wheel.x * 16;
                    var yDelta = _event.wheel.y * 16;

                    if (_event.wheel.direction == (uint) Sdl.SdlMouseWheelDirection.SdlMousewheelFlipped)
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

                    break;
                case Sdl.SdlEventType.SdlTextinput:
                    string text;

                    unsafe
                    {
                        var count = Sdl.SdlTextinputeventTextSize;
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
                        var ckey = (Keys) character;
                        DispatchKeyEvent(ckey, character, false, false, true);
                    }

                    break;
                case Sdl.SdlEventType.SdlMousemotion:
                    ReportMousePosition(_event.motion.x, _event.motion.y);
                    break;
            }
        }

        private MouseButton MapSdlMouseButton(uint button)
        {
            return button switch
            {
                Sdl.SdlButtonLeft => MouseButton.Primary,
                Sdl.SdlButtonRight => MouseButton.Secondary,
                Sdl.SdlButtonMiddle => MouseButton.Middle,
                Sdl.SdlButtonX1 => MouseButton.BrowserForward,
                Sdl.SdlButtonX2 => MouseButton.BrowserBack,
                _ => throw new NotSupportedException()
            };
        }
        
        protected override void OnWindowTitleChanged()
        {
            Sdl.SDL_SetWindowTitle(_sdlWindow, Title);
        }

        private uint GetWindowModeFlags()
        {
            var flags = 0x00u;

            if (IsBorderless)
            {
                flags |= (uint) Sdl.SdlWindowFlags.SdlWindowBorderless;
            }

            if (IsFullScreen)
            {
                if (IsBorderless)
                    flags |= (uint) Sdl.SdlWindowFlags.SdlWindowFullscreenDesktop;
                else
                    flags |= (uint) Sdl.SdlWindowFlags.SdlWindowFullscreen;
            }

            return flags;
        }

        protected override void OnWindowModeChanged()
        {
            // DestroySdlWindow();
            // CreateSdlWindow();

            var fsFlags = 0u;
            if (IsBorderless && IsFullScreen)
                fsFlags |= (uint) Sdl.SdlWindowFlags.SdlWindowFullscreen;
            else if (IsFullScreen)
                fsFlags |= (uint) Sdl.SdlWindowFlags.SdlWindowFullscreenDesktop;
            
            Sdl.SDL_SetWindowBordered(_sdlWindow, IsBorderless ? Sdl.SdlBool.SdlTrue : Sdl.SdlBool.SdlFalse);
            Sdl.SDL_SetWindowFullscreen(_sdlWindow, fsFlags);

            if (fsFlags > 0)
            {
                Sdl.SDL_SetWindowPosition(_sdlWindow, Sdl.SdlWindowposCentered, Sdl.SdlWindowposCentered);
            }
            
            base.OnWindowModeChanged();
        }

        protected override void OnClientSizeChanged()
        {
            // Resize the SDL window.
            Sdl.SDL_SetWindowSize(_sdlWindow, Width, Height);
            Sdl.SDL_SetWindowPosition(_sdlWindow, Sdl.SdlWindowposCentered, Sdl.SdlWindowposCentered);

            ReportClientSize(Width, Height);
            base.OnClientSizeChanged();
        }

        private void CreateSdlWindow()
        {
            var flags = GetWindowModeFlags();
            flags |= (uint) Sdl.SdlWindowFlags.SdlWindowOpengl;
            flags |= (uint) Sdl.SdlWindowFlags.SdlWindowShown;
            
            App.Logger.Log("Creating an SDL Window...");
            _sdlWindow = Sdl.SDL_CreateWindow(Title, 0, 0, Width, Height,
                (Sdl.SdlWindowFlags) flags);
            App.Logger.Log("SDL window is up. (640x480, SDL_WINDOW_SHOWN | SDL_WINDOW_OPENGL)");

            SetupGlRenderer();
        }

        private void DestroySdlWindow()
        {
            App.Logger.Log("Destroying current GL renderer...");
            Sdl.SDL_GL_DeleteContext(_glContext);
            _glContext = IntPtr.Zero;
            _graphicsProcessor = null;
            
            App.Logger.Log("Destroying the SDL window...");
            Sdl.SDL_DestroyWindow(_sdlWindow);
            
            // Fucking. STOP.
            _audio.Dispose();
        }
    }
}