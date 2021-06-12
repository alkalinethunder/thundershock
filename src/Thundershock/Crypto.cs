using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Thundershock
{
    public static class Crypto
    {
        public static string Sha256Hash(byte[] bytes)
        {
            using var sha256 = new SHA256CryptoServiceProvider();

            var fileHashBytes = sha256.ComputeHash(bytes);

            var dataHash = string.Concat(fileHashBytes.Select(x => x.ToString("X2")));

            return dataHash;
        }
        
        public static bool Sha256CompareFile(string path, string hash)
        {
            if (!File.Exists(path))
                return false;

            var bytes = File.ReadAllBytes(path);

            using var sha256 = new SHA256CryptoServiceProvider();

            var fileHashBytes = sha256.ComputeHash(bytes);

            var dataHash = string.Concat(fileHashBytes.Select(x => x.ToString("X2")));

            return dataHash == hash;
        }
    }
}