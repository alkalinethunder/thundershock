using System.Diagnostics.Tracing;

namespace Thundershock.Core.Rendering
{
    /// <summary>
    /// Defines the format of the depth buffer when creating a <see cref="RenderTarget"/>.
    /// </summary>
    public enum DepthFormat
    {
        /// <summary>
        /// No depth buffer is created.
        /// </summary>
        None,
        
        /// <summary>
        /// 24 bits per pixel are allocated for depth information.
        /// </summary>
        Depth24,
        
        /// <summary>
        /// 24 bits per pixel are allocated for depth information, and 8 are allocated for
        /// stencil information. 
        /// </summary>
        Depth24Stencil8
    }
}