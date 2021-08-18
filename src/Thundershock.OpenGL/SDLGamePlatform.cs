using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.OpenGL;
using Thundershock.Core;
using Thundershock.Core.Audio;
using Thundershock.Core.Debugging;
using Thundershock.Core.Rendering;

namespace Thundershock.OpenGL
{
    public sealed class SdlGamePlatform : IGamePlatform
    {
        private GL _gl;
        
        public SdlGamePlatform()
        {
            Logger.GetLogger().Log("Initializing SDL2...");
            var errno = Sdl.SDL_Init(Sdl.SdlInitVideo);
            if (errno != 0)
            {
                Logger.GetLogger().Log("SDL initialization HAS FAILED.", LogLevel.Fatal);
                var errText = Sdl.SDL_GetError();

                throw new Exception(errText);
            }
            
            // Create the Silk.NET GL context.
            _gl = GL.GetApi(Sdl.SDL_GL_GetProcAddress);
        }

        public GraphicsProcessor CreateGraphicsProcessor()
        {
            // GL debug mode.
#if DEBUG
            _gl.Enable(EnableCap.DebugOutput);
            _gl.DebugMessageCallback(PrintGlError, 0);
#endif

            return new GlGraphicsProcessor(_gl);
        }
        
        public string GraphicsCardDescription
        {
            get
            {
                var vendorString = string.Empty;
                var rendererString = string.Empty;

                unsafe
                {
                    var vendor = _gl.GetString(GLEnum.Vendor);
                    var renderer = _gl.GetString(GLEnum.Renderer);

                    var vendorLength = 0;
                    var ptr = vendor;
                    while ((*ptr) != 0)
                    {
                        ptr++;
                        vendorLength++;
                    }

                    var rendererLength = 0;
                    ptr = renderer;
                    while ((*ptr) != 0)
                    {
                        ptr++;
                        rendererLength++;
                    }

                    var vspan = new ReadOnlySpan<byte>(vendor, vendorLength);
                    var rspan = new ReadOnlySpan<byte>(renderer, rendererLength);

                    vendorString = Encoding.UTF8.GetString(vspan);
                    rendererString = Encoding.UTF8.GetString(rspan);
                }

                return vendorString + " " + rendererString;
            }
        }
        
        public int GetMonitorCount()
        {
            return Sdl.SDL_GetNumVideoDisplays();
        }

        public DisplayMode GetDefaultDisplayMode(int monitor)
        {
            var bounds = new Sdl.SdlRect();
            var result = Sdl.SDL_GetDisplayBounds(monitor, out bounds);

            if (result != 0)
                throw new Exception(Sdl.SDL_GetError());

            return new DisplayMode(bounds.w, bounds.h, monitor, bounds.x, bounds.y);
        }

        public IEnumerable<DisplayMode> GetAvailableDisplayModes(int monitor)
        {
            var count = Sdl.SDL_GetNumDisplayModes(monitor);
            for (var i = 0; i < count; i++)
            {
                Sdl.SDL_GetDisplayMode(monitor, i, out var mode);
                yield return new DisplayMode(mode.w, mode.h, monitor, 0, 0);
            }
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

            
            Logger.GetLogger().Log(messageString, logLevel);
        }
#endif
    }
}