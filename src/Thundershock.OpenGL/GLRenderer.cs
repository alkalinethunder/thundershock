using System;
using Thundershock.Core;
using Thundershock.Core.Rendering;

using static OpenGL.GL;

namespace Thundershock.OpenGL
{
    public sealed class GLRenderer : Renderer
    {
        internal GLRenderer() {}


        public override void Clear(Color color)
        {
            var vec4 = color.ToVector4();
            glClearColor(vec4.X, vec4.Y, vec4.Z, vec4.W);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        }
    }
}