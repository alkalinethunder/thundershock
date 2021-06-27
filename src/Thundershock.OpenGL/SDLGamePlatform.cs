using System;
using System.Collections.Generic;
using System.Text;
using SDL2;
using Silk.NET.OpenGL;
using Thundershock.Core;
using Thundershock.Core.Audio;

namespace Thundershock.OpenGL
{
    public sealed class SDLGamePlatform : IGamePlatform
    {
        private GL _gl;
        private OpenAlAudioBackend _audio;

        public AudioBackend Audio => _audio;
        
        internal SDLGamePlatform(GL gl, OpenAlAudioBackend al)
        {
            _gl = gl;
            _audio = al;
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

        public IEnumerable<DisplayMode> GetAvailableDisplayModes(int monitor)
        {
            var count = SDL.SDL_GetNumDisplayModes(monitor);
            for (var i = 0; i < count; i++)
            {
                SDL.SDL_GetDisplayMode(monitor, i, out var mode);
                yield return new DisplayMode(mode.w, mode.h, monitor, 0, 0);
            }
        }
    }
}