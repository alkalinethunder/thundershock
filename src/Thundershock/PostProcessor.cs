using System;
using System.Numerics;
using Thundershock.Core;
using Thundershock.Core.Rendering;

namespace Thundershock
{
    public class PostProcessor
    {
        #region Scene Settings

        public PostProcessSettings Settings { get; }

        #endregion
        
        #region State

        private GraphicsProcessor _gpu;
        private RenderTarget2D _effectBuffer1;
        private RenderTarget2D _effectBuffer2;
        private RenderTarget2D _intermediate;

        #endregion

        #region Resources

        private Effect _brightnessThreshold;
        private Effect _gaussian;
        private Effect _bloom;
        private Effect _shadowmask;
        private Effect _glitch;
        
        #endregion

        #region Shader Constants

        private const int KERNEL_SIZE = 15;

        #endregion

        #region Shader Parameters - Gaussian Blur

        private float[] _gaussianKernel = new float[KERNEL_SIZE]
        {
            0,
            0,
            0.000003f,
            0.000229f,
            0.005977f,
            0.060598f,
            0.24173f,
            0.382925f,
            0.24173f,
            0.060598f,
            0.005977f,
            0.000229f,
            0.000003f,
            0,
            0
        };
        private Vector2[] _offsets = new Vector2[KERNEL_SIZE];

        #endregion
        
        #region Shader Parameters - Bloom

        private float _baseIntensity = 1;
        private float _baseSaturation = 1;
        private float _bloomIntensity = 0.56f;
        private float _bloomSaturation = 1;
        private float _bloomThreshold = 0.59f;
        private float _blurAmount = 1.16f;

        #endregion

        #region Shader Parameters - CRT Shadow-mask

        private float _hardPix = -6;
        private float _hardScan = -10;
        private float _shadowmaskBrightness = 1f;
        private float _maskDark = 0.78f;
        private float _maskLight = 1.3f;
            
        #endregion

        #region Shader Parameters - Glitch Effects

        private float _glitchIntensity;
        private float _glitchSkew;

        #endregion

        #region Global Settings

        public bool EnableShadowMask { get; set; } = true;
        public bool EnableBloom { get; set; } = true;

        #endregion
        
        public PostProcessor(GraphicsProcessor gpu)
        {
            Settings = new PostProcessSettings(this);
            _gpu = gpu;
        }

        public void UnloadContent()
        {
            // shaders
            // _bloom.Dispose();
            // _shadowmask.Dispose();
            // _glitch.Dispose();
            
            // effect buffers
            _intermediate.Dispose();
            _effectBuffer1.Dispose();
            _effectBuffer2.Dispose();
            
            // null
            _bloom = null;
            _shadowmask = null;
            _glitch = null;
            _intermediate = null;
            _effectBuffer1 = null;
            _effectBuffer2 = null;
        }

        public void LoadContent()
        {
            /* _brightnessThreshold.Parameters["Threshold"].SetValue(_bloomThreshold);
            // _gaussian.Parameters["Kernel"].SetValue(_gaussianKernel);

            // _bloom.Parameters["BaseIntensity"].SetValue(_baseIntensity);
            // _bloom.Parameters["BloomIntensity"].SetValue(_bloomIntensity);

            // _bloom.Parameters["BloomSaturation"].SetValue(_bloomSaturation);
            // _bloom.Parameters["BaseSaturation"].SetValue(_baseSaturation); */
        }

        public void ReallocateEffectBuffers()
        {
            _effectBuffer1?.Dispose();
            _effectBuffer2?.Dispose();
            _intermediate?.Dispose();

            // _effectBuffer1 = new RenderTarget2D(_gfx, _gfx.PresentationParameters.BackBufferWidth,
//                _gfx.PresentationParameters.BackBufferHeight);
            //          _effectBuffer2 = new RenderTarget2D(_gfx, _gfx.PresentationParameters.BackBufferWidth,
            //            _gfx.PresentationParameters.BackBufferHeight);
            //      _intermediate = new RenderTarget2D(_gfx, _gfx.PresentationParameters.BackBufferWidth,
            //        _gfx.PresentationParameters.BackBufferHeight);

        }

        private void SetBlurOffsets(float dx, float dy)
        {
            _offsets[0] = Vector2.Zero;
            _gaussianKernel[0] = ComputeGaussian(0);

            float totalWeight = _gaussianKernel[0];
            
            for (var i = 0; i < KERNEL_SIZE / 2; i++)
            {
                float weight = ComputeGaussian(i + 1);
                float offset = i * 2 + 1.0f;

                totalWeight += weight;

                _gaussianKernel[i * 2 + 1] = weight;
                _gaussianKernel[i * 2 + 2] = weight;
                
                var delta = new Vector2(dx, dy) * offset;
                _offsets[i * 2 + 1] = delta;
                _offsets[i * 2 + 2] = -delta;
            }

            for (var i = 0; i < KERNEL_SIZE; i++)
            {
                _gaussianKernel[i] /= totalWeight;
            }

            // _gaussian.Parameters["Kernel"].SetValue(_gaussianKernel);
            // _gaussian.Parameters["Offsets"].SetValue(_offsets);
        }

        private float ComputeGaussian(float n)
        {
            float theta = _blurAmount;

            return (float)((1.0 / Math.Sqrt(2 * Math.PI * theta)) *
                           Math.Exp(-(n * n) / (2 * theta * theta)));
        }
        
        private void SetBloomTexture(Texture2D texture)
        {
            // _bloom.Parameters["BloomTexture"].SetValue(texture);
        }

        private void PerformBloom(RenderTarget2D frame, Rectangle rect)
        {
            var hWidth = (float) rect.Width;
            var hHeight = (float) rect.Height;

            // _gfx.SetRenderTarget(_effectBuffer1);

            // _batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            // _brightnessThreshold.CurrentTechnique.Passes[0].Apply();
            // _batch.Draw(frame, rect, Color.White);
            // _batch.End();

            // _gfx.SetRenderTarget(_effectBuffer2);

            SetBlurOffsets(1.0f / hWidth, 0f);

            // _batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            // _gaussian.CurrentTechnique.Passes[0].Apply();
            // _batch.Draw(_effectBuffer1, rect, Color.White);
            // _batch.End();

            // _gfx.SetRenderTarget(_effectBuffer1);

            SetBlurOffsets(0f, 1f / hHeight);

            // _batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            // _gaussian.CurrentTechnique.Passes[0].Apply();
            // _batch.Draw(_effectBuffer2, rect, Color.White);
            // _batch.End();

            // _gfx.SetRenderTarget(_effectBuffer2);

            // SetBloomTexture(_effectBuffer1);

            // _batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            // _bloom.CurrentTechnique.Passes[0].Apply();
            // _batch.Draw(frame, rect, Color.White);
            // _batch.End();

            // _gfx.SetRenderTarget(_intermediate);

            // _batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            // _batch.Draw(_effectBuffer2, rect, Color.White);
            // _batch.End();
        }

        private void NoEffect(RenderTarget2D renderTarget, Rectangle rect)
        {
            // _gfx.SetRenderTarget(_intermediate);

            // _batch.Begin();
            // _batch.Draw(renderTarget, rect, Color.White);
            // _batch.End();
        }

        private void SetShadowMaskParams()
        {
            /*_shadowmask.Parameters["TextureSize"].SetValue(_intermediate.Bounds.Size.ToVector2());
            _shadowmask.Parameters["OutputSize"].SetValue(_intermediate.Bounds.Size.ToVector2());
            _shadowmask.Parameters["HardPix"].SetValue(_hardPix);
            _shadowmask.Parameters["HardScan"].SetValue(_hardScan);
            _shadowmask.Parameters["BrightnessBoost"].SetValue(_shadowmaskBrightness);
            _shadowmask.Parameters["MaskDark"].SetValue(_maskDark);
            _shadowmask.Parameters["MaskLight"].SetValue(_maskLight);*/
        }
        
        public void Process(RenderTarget2D renderTarget)
        {
            var rect = renderTarget.Bounds;

            if (EnableBloom && Settings.EnableBloom)
            {
                PerformBloom(renderTarget, rect);
            }
            else
            {
                NoEffect(renderTarget, rect);
            }

            if (Settings.EnableGlitch && _glitchIntensity > 0)
            {
                // update glitch settings.
                // _glitch.Parameters["Intensity"].SetValue(_glitchIntensity);
                // _glitch.Parameters["TextureSize"].SetValue(rect.Size.ToVector2());
                // _glitch.Parameters["Skew"].SetValue(_glitchSkew);
                
                // copy intermediate to effect buffer 1.
                // using the glitch effect.
                // _gfx.SetRenderTarget(_effectBuffer1);
                // _batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap);
                // _glitch.CurrentTechnique.Passes[0].Apply();
                // _batch.Draw(_intermediate, rect, Color.White);
                // _batch.End();
                
                // render effect buffer 1 to intermediate
                // _gfx.SetRenderTarget(_intermediate);
                // _batch.Begin();
                // _batch.Draw(_effectBuffer1, rect, Color.White);
                // _batch.End();
            }

            if (EnableShadowMask && Settings.EnableShadowMask)
            {
                SetShadowMaskParams();
                
                // copy the intermediate RT to effect buffer 1
                // with the shadowmask effect applied
                // _gfx.SetRenderTarget(_effectBuffer1);
                // _batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                // _shadowmask.CurrentTechnique.Passes[0].Apply();
                // _batch.Draw(_intermediate, rect, Color.White);
                // _batch.End();
                
                // copy effect buffer 1 back into the intermediate buffer
                // _gfx.SetRenderTarget(_intermediate);
                // _batch.Begin();
                // _batch.Draw(_effectBuffer1, rect, Color.White);
                // _batch.End();
            }
            
            // _gfx.SetRenderTarget(null);

            // _batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap);
            // _batch.Draw(_intermediate, rect, Color.White);
            // _batch.End();
        }

        public class PostProcessSettings
        {
            private PostProcessor _processor;

            public bool EnableBloom { get; set; } = true;
            public bool EnableShadowMask { get; set; } = true;
            public bool EnableGlitch { get; set; }
            
            public float GlitchIntensity
            {
                get => _processor._glitchIntensity;
                set => _processor._glitchIntensity = value;
            }
            
            public float GlitchSkew
            {
                get => _processor._glitchSkew;
                set => _processor._glitchSkew = value;
            }
            
            public PostProcessSettings(PostProcessor processor)
            {
                _processor = processor;
            }
        }
    }
}