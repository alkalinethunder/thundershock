using Thundershock.Core;
using Thundershock.Rendering;

namespace Thundershock.Components
{
    /// <summary>
    /// Provides a camera component and the associated information for use on a Scene Object by the engine's
    /// rendering and post-processing system.
    /// </summary>
    public class CameraComponent
    {
        #region Projection Mode

        /// <summary>
        /// Defines whether this camera uses  an orthographic (2D) or perspective (3D) projection matrix. 
        /// </summary>
        public CameraProjectionType ProjectionType = CameraProjectionType.Orthographic;
        
        /// <summary>
        /// Defines the width, in screen units, of the camera's orthographic view if <see cref="ProjectionType"/> is
        /// set to Orthographic.
        /// </summary>
        public float OrthoWidth = 3840;

        /// <summary>
        /// Defines the height, in screen units, of the camera's orthographic view if <see cref="ProjectionType"/> is
        /// set to Orthographic.
        /// </summary>
        public float OrthoHeight = 2160;
        
        #endregion

        #region Post-Process Settings: Bloom

        /// <summary>
        /// Defines the intensity of the scene during bloom post-processing.
        /// </summary>
        public float BloomBaseIntensity = 1;
        
        /// <summary>
        /// Defines the saturation of colors in the scene during bloom post-processing.
        /// </summary>
        public float BloomBaseSaturation = 1;
        
        /// <summary>
        /// Defines the intensity of light in the scene when that light is bloomed.
        /// </summary>
        public float BloomIntensity = 0.56f;
        
        /// <summary>
        /// Defines the saturation of colors emitted from bloomed objects in the scene.
        /// </summary>
        public float BloomSaturation = 1;
        
        /// <summary>
        /// Defines the brightness threshold at which objects in the scene will be bloomed.
        /// </summary>
        public float BloomThreshold = 0.59f;
        
        /// <summary>
        /// Defines the amount of blur (light smearing) that's added to objects when being bloomed.
        /// </summary>
        public float BloomBlurAmount = 1.16f;

        #endregion

        #region Post-Process Settings: CRT Shadow-mask

        /// <summary>
        /// CRT hard pixel offset.
        /// </summary>
        public float CrtHardPix = -6;
        
        /// <summary>
        /// CRT hard scan offset.
        /// </summary>
        public float CrtHardScan = -10;
        
        /// <summary>
        /// Defines the over-all brightness of the CRT shadow-mask effect.
        /// </summary>
        public float CrtShadowMaskBrightness = 1;
        
        /// <summary>
        /// Defines the brightness of the darker areas of the CRT shadow mask.
        /// </summary>
        public float CrtMaskDark = 0.78f;
        
        /// <summary>
        /// Defines the brightness of the lighter areas of the CRT shadow mask.
        /// </summary>
        public float CrtMaskLight = 1.3f;

        #endregion

        #region Post-Process Effect Enablers

        /// <summary>
        /// Defines whether bloom post-process effect is enabled or disabled when this camera is active.
        /// </summary>
        public bool EnableBloom = true;
        
        /// <summary>
        /// Defines whether the CRT shadow-mask effect is enabled or disabled when this camera is active.
        /// </summary>
        public bool EnableCrt = true;

        #endregion

        #region World

        /// <summary>
        /// Defines a solid color used as the world's sky color.
        /// </summary>
        public Color SkyColor { get; set; } = Color.Black;

        #endregion
    }
}