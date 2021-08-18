using System;
using System.Collections.Generic;
using System.Linq;
using Thundershock.Core.Audio;
using Thundershock.Core.Debugging;
using Thundershock.Core.Rendering;

namespace Thundershock.Core
{
    [CheatAlias("Sys")]
    public static class GamePlatform
    {
        private static GraphicsProcessor _gpu;
        private static AudioBackend _audioBackend = new NullAudioBackend();
        private static IGamePlatform _gamePlatform;

        public static GraphicsProcessor GraphicsProcessor
        {
            get
            {
                if (_gpu == null)
                    throw new InvalidOperationException("Graphics is not currently enabled.");
                return _gpu;
            }
        }
        
        public static AudioBackend Audio => _audioBackend;
        
        public static int MonitorCount => _gamePlatform.GetMonitorCount();

        public static void InitializeGraphics()
        {
            if (_gpu != null)
                throw new InvalidOperationException("Graphics have already been initialized.");

            if (_gamePlatform == null)
                throw new InvalidOperationException("Game platform has not been initialized.");

            _gpu = _gamePlatform.CreateGraphicsProcessor();
        }
        
        public static DisplayMode GetDefaultDisplayMode(int monitor)
        {
            return _gamePlatform.GetDefaultDisplayMode(monitor);
        }
        
        public static string GraphicsCardDescription => _gamePlatform.GraphicsCardDescription;

        public static void InitializeAudio<T>() where T : AudioBackend, new()
        {
            var backend = new T();

            _audioBackend = backend;
        }
        
        public static IEnumerable<DisplayMode> GetAvailableDisplayModes(int monitor)
        {
            return _gamePlatform.GetAvailableDisplayModes(monitor).Distinct();
        }
        public static void Initialize(IGamePlatform gamePlatform)
        {
            _gamePlatform = gamePlatform;
        }

        public static DisplayMode GetDisplayMode(string resolution, int monitor)
        {
            if (monitor < 0 || monitor > MonitorCount)
            {
                Logger.GetLogger()
                    .Log("Config-specified monitor isn't valid so we're going to use the system's primary display.",
                        LogLevel.Warning);
                monitor = 0;
            }

            if (ParseDisplayMode(resolution, out var width, out var height))
            {
                Logger.GetLogger().Log($"Available display modes for monitor {monitor} on {GraphicsCardDescription}:");

                var result = DisplayMode.Invalid;
                
                foreach (var mode in GetAvailableDisplayModes(monitor))
                {
                    Logger.GetLogger().Log($" - {mode.Width}x{mode.Height} ({mode.MonitorX}, {mode.MonitorY})");
                    if (result.IsInvalid && mode.Width == width && mode.Height == height)
                    {
                        result = mode;
                    }
                }

                if (result.IsInvalid)
                {
                    Logger.GetLogger()
                        .Log(
                            "Specified display mode " + resolution +
                            " not supported on this display, using default mode", LogLevel.Warning);
                    result = GetDefaultDisplayMode(monitor);
                }

                return result;
            }
            else
            {
                Logger.GetLogger()
                    .Log(
                        "Couldn't parse display resolution string " + resolution +
                        " into anything sensible - we're going to use the native screen resolution for this display.",
                        LogLevel.Warning);
                return GetDefaultDisplayMode(monitor);
            }
        }
        
        public static bool ParseDisplayMode(string displayMode, out int width, out int height)
        {
            var result = false;

            width = 0;
            height = 0;

            if (!string.IsNullOrWhiteSpace(displayMode))
            {
                var lowercase = displayMode.ToLower();

                var x = 'x';

                if (lowercase.Contains(x))
                {
                    var index = lowercase.IndexOf(x);

                    var wString = lowercase.Substring(0, index);
                    var hString = lowercase.Substring(index + 1);

                    if (int.TryParse(wString, out width) && int.TryParse(hString, out height))
                    {
                        result = true;
                    }
                }
            }
            
            return result;
        }

        [Cheat("GPUName")]
        public static void PrintGpuInfo()
        {
            Logger.GetLogger().Log(GraphicsCardDescription);
        }
    }

    public class NullAudioBackend : AudioBackend
    {
        public override float MasterVolume { get; set; }
        public override IAudioBuffer CreateBuffer(int channels, int sampleRate)
        {
            return new NullAudioBuffer();
        }

        public override AudioOutput OpenAudioOutput()
        {
            return new NullAudioOutput();
        }

        protected override void Dispose(bool disposing)
        { }
    }

    public class NullAudioBuffer : IAudioBuffer
    {
        public void Dispose()
        {
        }

        public uint Id => 0;
        public int Channels => 0;
        public int SampleRate => 0;
        public double Power => 0;
        public ReadOnlySpan<byte> Buffer => Array.Empty<byte>();
        public void SetBuffer(ReadOnlySpan<byte> buffer)
        { }
    }

    public class NullAudioOutput : AudioOutput
    {
        public override int PendingBufferCount => 0;
        public override double Power => 0;
        public override AudioState State => AudioState.Stopped;
        public override void Stop()
        {
        }

        public override float Volume { get; set; }
        protected override void Dispose(bool disposing)
        {
        }

        public override void Play()
        { }

        public override void SubmitBuffer(IAudioBuffer buffer)
        { }
    }
}