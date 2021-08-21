using System.IO;
using Thundershock.Content;
using Thundershock.Core;
using Thundershock.Core.Rendering;
using Thundershock.IO;

namespace Thundershock
{
    public static class AssetManager
    {
        private static AssetManagerNode _node = new();

        private static FileSystem GetFS()
        {
            return FileSystem.FromNode(_node);
        }

        public static bool TryLoadTexture(string path, out Texture2D texture)
        {
            var fs = GetFS();

            if (fs.FileExists(path))
            {
                texture = Texture2D.FromPak(GamePlatform.GraphicsProcessor, fs.OpenFile(path));
                return true;
            }
            else
            {
                texture = null;
                return false;
            }
        }
        
        public static bool TryLoadImage(string path, out Texture2D texture)
        {
            var fs = GetFS();

            if (fs.FileExists(path))
            {
                texture = Texture2D.FromStream(GamePlatform.GraphicsProcessor, fs.OpenFile(path));
                return true;
            }
            else
            {
                texture = null;
                return false;
            }
        }

        public static void AddThirdPartyPak(string mount, string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            var pakFile = PakUtils.OpenPak(path);

            _node.AddPak(mount, pakFile);
        }
    }
}