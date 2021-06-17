using OpenGL;
using Thundershock.Core.Rendering;

namespace Thundershock.OpenGL
{
    public sealed class GlTextureCollection : TextureCollection
    {
        private Texture[] _textures;
        
        public GlTextureCollection(GraphicsProcessor gpu) : base(gpu)
        {
            _textures = new Texture[32];
        }

        public override int Count => _textures.Length;
        protected override Texture GetTexture(int index)
        {
            return _textures[index];
        }

        protected override void BindTexture(int index, Texture texture)
        {
            GL.glActiveTexture(index);
            if (texture == null)
            {
                GL.glBindTexture(GL.GL_TEXTURE_2D, 0);
            }
            else
            {
                GL.glBindTexture(GL.GL_TEXTURE_2D, texture.Id);
            }

            _textures[index] = texture;
        }
    }
}