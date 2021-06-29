using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NVorbis;
using Thundershock.Core;
using Thundershock.Core.Audio;

namespace Thundershock.Audio
{
    public class Sound : IDisposable
    {
        private AudioOutput _output;
        private List<IAudioBuffer> _buffers;

        public AudioState State => _output.State;
        
        public double Power => _output.Power;
        
        public float Volume
        {
            get => _output.Volume;
            set => _output.Volume = value;
        }

        public void Play()
        {
            Stop();

            foreach (var buf in _buffers)
                _output.SubmitBuffer(buf);
            
            _output.Play();
        }

        private void Stop()
        {
            _output.Stop();
        }

        public void Dispose()
        {
            _output.Stop();
        }
        
        private Sound(List<IAudioBuffer> buffers)
        {
            _buffers = buffers;

            _output = GamePlatform.Audio.OpenAudioOutput();
        }

        private static Sound CreateSound(IMusicReader reader)
        {
            var buffers = new List<IAudioBuffer>();

            var readCount = 1024 * reader.Channels;
            var read = Array.Empty<byte>();

            do
            {
                var buf = GamePlatform.Audio.CreateBuffer(reader.Channels, reader.SampleRate);

                read = reader.ReadSamples(readCount);

                buf.SetBuffer(read);

                buffers.Add(buf);
            } while (read.Length >= readCount);

            return new Sound(buffers);
        }
        
        public static Sound FromOggStream(Stream stream)
        {
            using var reader = new VorbisReader(stream);
            var musicReader = new VorbisStream(reader);

            return CreateSound(musicReader);
        }

        public static Sound FromOggResource(Assembly ass, string resource)
        {
            if (Resource.GetStream(ass, resource, out var stream))
                return FromOggStream(stream);
            throw new InvalidOperationException("Resource not found.");
        }

        public static Sound FromOggFile(string file)
        {
            return FromOggStream(File.OpenRead(file));
        }
    }
}