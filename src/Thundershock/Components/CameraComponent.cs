using Thundershock.Core;
using Thundershock.Rendering;

namespace Thundershock.Components
{
    public class CameraComponent
    {
        #region Projection Mode

        public CameraProjectionType ProjectionType = CameraProjectionType.Perspective;

        #endregion

        #region Post-Process Settings: Bloom

        public float BloomBaseIntensity = 1;
        public float BloomBaseSaturation = 1;
        public float BloomIntensity = 0.56f;
        public float BloomSaturation = 1;
        public float BloomThreshold = 0.59f;
        public float BloomBlurAmount = 1.16f;

        #endregion

        #region Post-Process Settings: CRT Shadow-mask

        public float CRTHardPix = -6;
        public float CRTHardScan = -10;
        public float CRTShadowMaskBrightness = 1;
        public float CRTMaskDark = 0.78f;
        public float CRTMaskLight = 1.3f;

        #endregion

        #region Post-Process Effect Enablers

        public bool EnableBloom = true;
        public bool EnableCRT = true;

        #endregion

        #region World

        public Color BackgroundColor { get; set; } = Color.Black;

        #endregion
    }
}