using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Thundershock.Core
{
    public static class Crypto
    {
        // https://stackoverflow.com/questions/9545619/a-fast-hash-function-for-string-in-c-sharp
        public static ulong KnuthHash(string read)
        {
            var hashedValue = 3074457345618258791ul;

            for (var i = 0; i < read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= 3074457345618258799ul;
            }
            
            return hashedValue;
        }
        
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