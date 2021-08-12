using System;
using System.Linq;
using System.Numerics;
using Thundershock.Components;
using Thundershock.Core;
using Thundershock.Core.Debugging;
using Thundershock.Core.Rendering;

namespace Thundershock
{
    public class PostProcessor
    {
        #region Post-Processor Quad

        private readonly Vertex[] _verts = new Vertex[]
        {
            new Vertex(new Vector3(0, 0, 0), Color.White, new Vector2(0, 1)),
            new Vertex(new Vector3(1, 0, 0), Color.White, new Vector2(1, 1)),
            new Vertex(new Vector3(0, 1, 0), Color.White, new Vector2(0, 0)),
            new Vertex(new Vector3(1, 1, 0), Color.White, new Vector2(1, 0)),
        };

        private readonly int[] _indices = new int[]
        {
            1, 2, 0,
            1, 3, 2
        };

        #endregion
        
        #region Scene Settings

        public PostProcessSettings Settings { get; }

        #endregion
        
        #region State

        private bool _ignoreCamera;
        private Matrix4x4 _matrix;
        private GraphicsProcessor _gpu;
        private BasicEffect _basicEffect;
        private RenderTarget2D _effectBuffer1;
        private RenderTarget2D _effectBuffer2;
        private RenderTarget2D _intermediate;

        #endregion

        #region Resources

        private Effect _ppEffect;
        private Effect.EffectProgram _fxaa;
        private Effect.EffectProgram _brightnessThreshold;
        private Effect.EffectProgram _gaussian;
        private Effect.EffectProgram _bloom;
        private Effect.EffectProgram _shadowmask;

        #endregion

        #region Shader Constants

        private const int KernelSize = 15;

        #endregion

        #region Shader Parameters - Gaussian Blur

        private float[] _gaussianKernel =
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
        private Vector2[] _offsets = new Vector2[KernelSize];

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
            _basicEffect = new(_gpu);
            _matrix = Matrix4x4.CreateOrthographicOffCenter(0, 1, 1, 0, -1, 1);

            _basicEffect.Programs.First().Parameters["projection"].SetValue(_matrix);
        }

        public void SettingsFromCameraComponent(CameraComponent cam)
        {
            if (_ignoreCamera)
                return;
            
            _bloomThreshold = cam.BloomThreshold;
            _bloomIntensity = cam.BloomIntensity;
            _bloomSaturation = cam.BloomSaturation;
            _baseIntensity = cam.BloomBaseIntensity;
            _baseSaturation = cam.BloomBaseSaturation;
            _blurAmount = cam.BloomBlurAmount;
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
            _fxaa = null;
            _bloom = null;
            _shadowmask = null;
            _intermediate = null;
            _effectBuffer1 = null;
            _effectBuffer2 = null;
        }

        public void LoadContent()
        {
            if (Resource.TryGetString(GetType().Assembly, "Thundershock.Resources.Effects.PostProcessor.glsl",
                out var text))
            {
                _ppEffect = ShaderPipeline.CompileShader(_gpu, text);
            }

            _brightnessThreshold = _ppEffect.Programs["BloomThreshold"];
            _gaussian = _ppEffect.Programs["BloomGaussian"];
            _bloom = _ppEffect.Programs["Bloom"];
            _shadowmask = _ppEffect.Programs["CRT"];
            _fxaa = _ppEffect.Programs["FXAA"];
            
            /* _brightnessThreshold.Parameters["Threshold"].SetValue(_bloomThreshold);
            // _gaussian.Parameters["Kernel"].SetValue(_gaussianKernel);

            // _bloom.Parameters["BaseIntensity"].SetValue(_baseIntensity);
            // _bloom.Parameters["BloomIntensity"].SetValue(_bloomIntensity);

            // _bloom.Parameters["BloomSaturation"].SetValue(_bloomSaturation);
            // _bloom.Parameters["BaseSaturation"].SetValue(_baseSaturation); */

            _brightnessThreshold.Parameters["transform"].SetValue(_matrix);
            _gaussian.Parameters["transform"].SetValue(_matrix);
            _bloom.Parameters["transform"].SetValue(_matrix);
            _shadowmask.Parameters["transform"].SetValue(_matrix);
            _fxaa.Parameters["transform"].SetValue(_matrix);
        }

        public void ReallocateEffectBuffers(int width, int height)
        {
            _effectBuffer1?.Dispose();
            _effectBuffer2?.Dispose();
            _intermediate?.Dispose();

            _effectBuffer1 = new RenderTarget2D(_gpu, width, height);
            _effectBuffer2 = new RenderTarget2D(_gpu, width, height);
            _intermediate = new RenderTarget2D(_gpu, width, height);
        }

        private void SetBlurOffsets(float dx, float dy)
        {
            _offsets[0] = Vector2.Zero;
            _gaussianKernel[0] = ComputeGaussian(0);

            float totalWeight = _gaussianKernel[0];
            
            for (var i = 0; i < KernelSize / 2; i++)
            {
                var weight = ComputeGaussian(i + 1);
                var offset = i * 2 + 1.0f;

                totalWeight += weight;

                _gaussianKernel[i * 2 + 1] = weight;
                _gaussianKernel[i * 2 + 2] = weight;
                
                var delta = new Vector2(dx, dy) * offset;
                _offsets[i * 2 + 1] = delta;
                _offsets[i * 2 + 2] = -delta;
            }

            for (var i = 0; i < KernelSize; i++)
            {
                _gaussianKernel[i] /= totalWeight;
            }

            _gaussian.Parameters["weights"].SetValue(_gaussianKernel);
            _gaussian.Parameters["offsets"].SetValue(_offsets);
        }

        private float ComputeGaussian(float n)
        {
            float theta = _blurAmount;

            return (float)((1.0 / Math.Sqrt(2 * Math.PI * theta)) *
                           Math.Exp(-(n * n) / (2 * theta * theta)));
        }

        private void PerformFXAA(RenderTarget2D frame, Rectangle rect)
        {
            var hWidth = rect.Width;
            var hHeight = rect.Height;

            _gpu.SetRenderTarget(_effectBuffer1);
            _gpu.Clear(Color.Black);
            
            _fxaa.Parameters["inputSize"]?.SetValue(new Vector2(hWidth, hHeight));
            _fxaa.Apply();

            _gpu.PrepareRender();
            _gpu.Textures[0] = frame;
            
            _gpu.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);

            _gpu.EndRender();
            _gpu.Textures[0] = null;
            
            // Render to our intermediate buffer.
            _gpu.SetRenderTarget(_intermediate);
            _gpu.Clear(Color.Black);
            _basicEffect.Programs.First().Apply();
            _gpu.PrepareRender(BlendMode.Alpha);
            _gpu.Textures[0] = _effectBuffer1;
            _gpu.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            _gpu.EndRender();
            _gpu.Textures[0] = null;
            _gpu.SetRenderTarget(null);
        }
        
        private void PerformBloom(RenderTarget2D frame, Rectangle rect)
        {
            var hWidth = rect.Width;
            var hHeight = rect.Height;

            // change to the first effect buffer.
            _gpu.SetRenderTarget(_effectBuffer1);
            _gpu.Clear(Color.Black);
            
            // Apply the bloom threshold program.
            _brightnessThreshold.Parameters["threshold"].SetValue(_bloomThreshold);
            _brightnessThreshold.Apply();
            
            // Prepare for a render.
            _gpu.PrepareRender();
            _gpu.Textures[1] = frame;
            
            // Render the quad.
            _gpu.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            
            // End the render.
            _gpu.EndRender();
            _gpu.Textures[1] = null;

            // Now we switch to Effect Buffer 2 so we can render the first blur pass.
            _gpu.SetRenderTarget(_effectBuffer2);
            _gpu.Clear(Color.Black);

            SetBlurOffsets(1.0f / hWidth, 0f);

            // Apply the blur shader.
            _gaussian.Apply();
            
            // Prepare for a render.
            _gpu.PrepareRender();
            _gpu.Textures[0] = _effectBuffer1;
            
            // Draw the quad.
            _gpu.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            
            // End the render.
            _gpu.EndRender();
            _gpu.Textures[0] = null;
            
            // Switch to Effect Buffer 1 again and render the first blur pass with
            // an additional blur pass.
            _gpu.SetRenderTarget(_effectBuffer1);
            _gpu.Clear(Color.Black);
            
            SetBlurOffsets(0f, 1f / hHeight);

            // Start the render after setting Effect Buffer 2 as the texture to render.
            _gpu.Textures[0] = _effectBuffer2;
            _gpu.PrepareRender();
            
            // Draw the quad.
            _gpu.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            
            // End the render.
            _gpu.EndRender();
            _gpu.Textures[0] = null;
            
            _gpu.SetRenderTarget(null);
            
            // Now for the actual bloom effect.
            _gpu.SetRenderTarget(_effectBuffer2);
            _gpu.Clear(Color.Black);
            
            _gpu.Textures[0] = frame;
            _gpu.Textures[1] = _effectBuffer1;

            _bloom.Parameters["baseIntensity"].SetValue(_baseIntensity);
            _bloom.Parameters["baseSaturation"].SetValue(_baseSaturation);
            _bloom.Parameters["bloomIntensity"].SetValue(_bloomIntensity);
            _bloom.Parameters["bloomSaturation"].SetValue(_bloomSaturation);
            
            _bloom.Apply();

            _gpu.PrepareRender();
            _gpu.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            _gpu.EndRender();

            _gpu.Textures[0] = null;
            _gpu.Textures[1] = null;

            // Render to our intermediate buffer.
            _gpu.SetRenderTarget(_intermediate);
            _gpu.Clear(Color.Black);
            _basicEffect.Programs.First().Apply();
            _gpu.PrepareRender(BlendMode.Additive);
            _gpu.Textures[0] = _effectBuffer2;
            _gpu.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            _gpu.EndRender();
            _gpu.Textures[0] = null;
            _gpu.SetRenderTarget(null);
        }

        private void NoEffect(RenderTarget2D renderTarget)
        {
            // Bind the render target as a texture.
            _gpu.Textures[0] = renderTarget;

            // Render to the intermediate buffer.
            _gpu.SetRenderTarget(_intermediate);
            
            // Use the basic effect shader program for this next render.
            // We don't want any special effects here. Just the source frame.
            _basicEffect.Programs.First().Apply();
            
            // Prepare the render.
            _gpu.PrepareRender();
            
            // Render the quad.
            _gpu.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            
            // Clean up.
            _gpu.EndRender();
            _gpu.SetRenderTarget(null);
            _gpu.Textures[0] = null;
        }

        private void SetShadowMaskParams()
        {
            _shadowmask.Parameters["texSize"].SetValue(_intermediate.Bounds.Size);
            _shadowmask.Parameters["outputSize"].SetValue(_intermediate.Bounds.Size);
            _shadowmask.Parameters["hardPix"].SetValue(_hardPix);
            _shadowmask.Parameters["hardScan"].SetValue(_hardScan);
            _shadowmask.Parameters["brightnessBoost"].SetValue(_shadowmaskBrightness);
            _shadowmask.Parameters["maskDark"].SetValue(_maskDark);
            _shadowmask.Parameters["maskLight"].SetValue(_maskLight);
        }
        
        public void Process(RenderTarget2D renderTarget)
        {
            // First step in all this is submitting our quad to the GPU.
            _gpu.SubmitVertices(_verts);
            _gpu.SubmitIndices(_indices);
            
            var rect = renderTarget.Bounds;

            // TODO: Ability to disable FXAA.
            PerformFXAA(renderTarget, rect);

            if (EnableBloom && Settings.EnableBloom)
            {
                PerformBloom(_intermediate, rect);
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
                _gpu.SetRenderTarget(_effectBuffer1);
                
                // apply the shadowmask.
                _shadowmask.Apply();
                
                // clear the effect buffer
                _gpu.Clear(Color.Black);
                
                // Bind the texture and prepare for a render
                _gpu.Textures[0] = _intermediate;
                _gpu.PrepareRender();

                // Render
                _gpu.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
                
                // End the render and bind the effect buffer after we switch to the immediate RT
                _gpu.EndRender();
                _gpu.SetRenderTarget(null);
                _gpu.Textures[0] = _effectBuffer1;
                _gpu.SetRenderTarget(_intermediate);
                
                // Apply the default effect.
                _basicEffect.Programs.First().Apply();
                
                // Start a render
                _gpu.PrepareRender();
                
                // Render
                _gpu.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
                
                // End.
                _gpu.EndRender();
                _gpu.Textures[0] = null;
                _gpu.SetRenderTarget(null);
            }
            
            // Render the intermediate buffer to the screen.
            _basicEffect.Programs.First().Apply();
            _gpu.SetRenderTarget(null);

            // Get the GPU ready for some rendering :P
            _gpu.PrepareRender();
            _gpu.Textures[0] = _intermediate;
            _gpu.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
            _gpu.EndRender();

            _gpu.Textures[0] = null;
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

        #region Cheats

        [Cheat("IgnoreCamera")]
        public void IgnoreCamera(bool value)
        {
            _ignoreCamera = value;
        }

        [Cheat("Bloom")]
        public void EnableBloomCheat(bool value)
        {
            Settings.EnableBloom = value;
        }

        [Cheat("CRT")]
        public void CrtCheat(bool value)
        {
            Settings.EnableShadowMask = value;
        }

        [Cheat("BloomThreshold")]
        public void BloomThresholdCheat(float value)
        {
            _bloomThreshold = value;
        }
        
        [Cheat("BloomIntensity")]
        public void BloomIntensityCheat(float value)
        {
            _bloomIntensity = value;
        }

        [Cheat("BloomSaturation")]
        public void BloomSaturationCheat(float value)
        {
            _bloomSaturation = value;
        }

        [Cheat("BloomBaseIntensity")]
        public void BloomBaseIntensityCheat(float value)
        {
            _baseIntensity = value;
        }

        [Cheat("BloomBaseSaturation")]
        public void BloomBaseSaturationCheat(float value)
        {
            _baseSaturation = value;
        }

        [Cheat("BloomBlur")]
        public void BloomBlurCheat(float value)
        {
            _blurAmount = value;
        }

        #endregion
    }
}