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

        private Song _currentSong;
        private Song _nextSong;
        
        private void Play(Song song, double fadeTime = 0)
        {
            if (fadeTime <= 0)
            {
                StopInternal();

                _currentSong = song;
                _playing = _backend.OpenAudioOutput(_currentSong.SampleRate, _currentSong.Channels);
                _playing.Play();
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

        internal void Update(double deltaTime)
        {
            if (_fade < _fadeTime && _fadeTime > 0)
            {
                _fade += deltaTime;

                var volume = (float) MathHelper.Clamp(_fade / _fadeTime, 0, 1);

                if (_playing != null)
                {
                    _playing.Volume = 1 - volume;
                }

                if (_next != null)
                {
                    _next.Volume = volume;
                }
            }
            else
            {
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

            if (_currentSong != null && _playing != null && _playing.PendingBufferCount < 3)
            {
                // read a frame from the song and submit it to the audio output.
                var frame = _currentSong.ReadFrame();
                _playing.SubmitBuffer(frame);
            }
        }
        

        private static MusicPlayer _instance;

        public static void PlaySong(Song song)
        {
            GetInstance().Play(song);
        }
        
        public static MusicPlayer GetInstance()
        {
            if (_instance != null)
                return _instance;

            _instance = new MusicPlayer(GamePlatform.Audio);
            return _instance;
        }

        [Cheat("PlayOggFile")]
        private static void PlayOggCheat(string file)
        {
            PlaySong(Song.FromOggFile(file));
        }
    }
}