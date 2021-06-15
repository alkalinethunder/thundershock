using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Thundershock.Config;

namespace Thundershock.Audio
{
    public class BgmManager : GameAppComponent
    {
        private ConfigurationManager _config;
        
        private SoundEffectInstance _currentPlayback;
        private SoundEffectInstance _nextPlayback;

        private bool _isFading;
        private double _fadeValue;
        private double _fadeTime;
        private float _masterVolume;
        
        protected override void OnLoad()
        {
            _config = App.GetComponent<ConfigurationManager>();

            LoadConfig();
            
            _config.ConfigurationLoaded += ConfigOnConfigurationLoaded;
            
            App.Logger.Log("Background Music Manager is ready.");
            base.OnLoad();
        }
        
        protected override void OnUnload()
        {
            Stop();
            base.OnUnload();
        }

        protected override void OnUpdate(Thundershock.Core.GameTime gameTime)
        {
            if (_isFading)
            {
                _fadeValue += gameTime.ElapsedGameTime.TotalSeconds;

                var volume = MathHelper.Clamp((float) (_fadeValue / _fadeTime), 0, 1);

                if (_currentPlayback != null)
                {
                    _currentPlayback.Volume = (1 - volume) * _masterVolume;
                }

                if (_nextPlayback != null)
                {
                    _nextPlayback.Volume = volume * _masterVolume;
                }
                
                if (_fadeTime >= _fadeValue)
                {
                    StopInternal(_currentPlayback);
                    _currentPlayback = _nextPlayback;
                    _nextPlayback = null;
                    _isFading = false;
                    _fadeTime = 0;
                    _fadeValue = 0;
                }
            }
            else
            {
                if (_currentPlayback != null)
                {
                    _currentPlayback.Volume = _masterVolume;
                }
            }
            
            base.OnUpdate(gameTime);
        }

        public void Play(SoundEffect sound)
        {
            Stop();

            _currentPlayback = sound.CreateInstance();
            _currentPlayback.Play();
        }
        
        public void Play(string path)
            => Play(App.Content.Load<SoundEffect>(path));
        
        public void PlayWithFade(SoundEffect sound, double fadeTime = 1)
        {
            StopInternal(_nextPlayback);

            _nextPlayback = sound.CreateInstance();
            _nextPlayback.Volume = 0;
            _nextPlayback.Play();
            
            _fadeValue = 0;
            _fadeTime = fadeTime;

            _isFading = true;
        }
        
        public void PlayWithFade(string path, double fadeTime = 1)
        {
            var sound = App.Content.Load<SoundEffect>(path);
            PlayWithFade(sound, fadeTime);
        }
        
        public void Stop()
        {
            StopInternal(_currentPlayback);
            StopInternal(_nextPlayback);

            _currentPlayback = null;
            _nextPlayback = null;
            
            _isFading = false;
            _fadeTime = 0;
            _fadeValue = 0;
        }

        private void StopInternal(SoundEffectInstance sfx)
        {
            if (sfx != null && !sfx.IsDisposed)
            {
                App.Logger.Log("Stopping playback of background audio.");
                sfx.Stop();
                sfx.Dispose();
            }
        }

        private void LoadConfig()
        {
            _masterVolume = MathHelper.Clamp(_config.ActiveConfig.BgmVolume, 0, 1);
        }
        
        private void ConfigOnConfigurationLoaded(object? sender, EventArgs e)
        {
            LoadConfig();
        }
    }
}