using System;
using Thundershock.Core;
using Thundershock.Core.Audio;
using Thundershock.Core.Debugging;

namespace Thundershock.Audio
{
    [CheatAlias("Music")]
    public class MusicPlayer
    {
        private AudioBackend _backend;
        private AudioOutput _playing;
        private AudioOutput _next;
        private double _fadeTime;
        private double _fade;
        private float _volume = 1;

        private Song _currentSong;
        private Song _nextSong;
        
        private void Play(Song song, double fadeTime = 0)
        {
            if (fadeTime <= 0)
            {
                StopInternal();

                _currentSong = song;

                _playing = _backend.OpenAudioOutput();
                
                ReadInitial(_currentSong, _playing);
                
                _playing.BufferProcessed += PlayingBufferProcessed;
                
                _playing.Play();
            }
            else
            {
                _fade = 0;
                _fadeTime = fadeTime;

                if (_next != null)
                {
                    _playing?.Dispose();
                    _currentSong?.Dispose();
                    _currentSong = _nextSong;
                    _playing = _next;
                }

                _nextSong = song;
                _next = _backend.OpenAudioOutput();

                ReadInitial(_nextSong, _next);
                _next.BufferProcessed += PlayingBufferProcessed;
                
                _next.Play();
            }
        }

        private void PlayingBufferProcessed(object sender, IAudioBuffer e)
        {
            if (sender is AudioOutput output)
            {
                var read = Array.Empty<byte>();

                if (output == _playing)
                {
                    read = _currentSong.ReadFrame();
                }
                else if (output == _next)
                {
                    read = _nextSong.ReadFrame();
                }
                
                if (read.Length > 0)
                {
                    e.SetBuffer(read);
                    output.SubmitBuffer(e);
                }
                else
                {
                    e.Dispose();
                }
            }
        }

        private void StopInternal()
        {
            _nextSong?.Dispose();
            _currentSong?.Dispose();
            _next?.Dispose();
            _playing?.Dispose();

            _fade = 0;
            _fadeTime = 0;

            _playing = null;
            _next = null;
            _currentSong = null;
            _nextSong = null;
        }
        
        private MusicPlayer(AudioBackend backend)
        {
            _backend = backend;
        }

        private double CalculatePower()
        {
            var result = 0d;
            var streams = 0;

            if (_playing != null)
            {
                streams++;
                result += _playing.Power * _playing.Volume;
            }

            if (_next != null)
            {
                streams++;
                result += _next.Power * _next.Volume;
            }
            
            return result / streams;
        }
        
        internal void Update(double deltaTime)
        {
            if (_fade < _fadeTime && _fadeTime > 0)
            {
                _fade += deltaTime;

                var volume = (float) MathHelper.Clamp(_fade / _fadeTime, 0, 1);

                if (_playing != null)
                {
                    _playing.Volume = (1 - volume) * _volume;
                }

                if (_next != null)
                {
                    _next.Volume = volume * _volume;
                }
            }
            else
            {
                if (_playing != null)
                    _playing.Volume = _volume;

                if (_next != null)
                    _next.Volume = _volume;
                
                if (_fadeTime > 0)
                {
                    _fade = 0;
                    _fadeTime = 0;

                    _playing?.Dispose();
                    _currentSong?.Dispose();
                    
                    if (_next != null)
                    {
                        _playing = _next;
                        _currentSong = _nextSong;

                        _nextSong = null;
                        _next = null;
                    }
                }
            }
        }
        

        private static MusicPlayer _instance;

        public static double Power => GetInstance().CalculatePower();

        public static float MasterVolume
        {
            get => GetInstance()._volume;
            set => GetInstance()._volume = MathHelper.Clamp(value, 0, 1);
        }

        public static void PlaySong(Song song, double fade = 0)
        {
            GetInstance().Play(song, fade);
        }

        public static MusicPlayer GetInstance()
        {
            if (_instance != null)
                return _instance;

            _instance = new MusicPlayer(GamePlatform.Audio);
            return _instance;
        }

        [Cheat("Volume")]
        public static void Cheat_SetVolume(float volume)
        {
            MasterVolume = volume;
        }
        
        [Cheat("PlayOggFade")]
        public static void PlayOggFade(double fadeTime, string file)
        {
            var song = Song.FromOggFile(file);
            GetInstance().Play(song, fadeTime);
        }
        
        [Cheat("PlayOggFile")]
        public static void PlayOggCheat(string file)
        {
            PlaySong(Song.FromOggFile(file));
        }

        private static void ReadInitial(Song song, AudioOutput output)
        {
            var bufCount = 4;

            while (bufCount >= 0)
            {
                var bug = GamePlatform.Audio.CreateBuffer(song.Channels, song.SampleRate); // I'm keeping that FUCKING typo.

                var data = song.ReadFrame();

                bug.SetBuffer(data);

                output.SubmitBuffer(bug);
                
                bufCount--;
            }
        }

        public static void Stop()
        {
            GetInstance().StopInternal();
        }
    }
}