using System.IO;
using System.Reflection;
using Thundershock.Core.Debugging;

namespace Thundershock.Core
{
    public static class Resource
    {
        public static bool GetStream(Assembly ass, string resourceName, out Stream stream)
        {
            stream = null;
            
            if (ass == null)
            {
                Logger.GetLogger().Log($"Tried to get an embedded resource from an invalid assembly.", LogLevel.Error);
                return false;
            }

            Logger.GetLogger()
                .Log($"Retrieving embedded resource " + resourceName + " from assembly " + ass);

            stream = ass.GetManifestResourceStream(resourceName);
            
            if (stream == null)
            {
                Logger.GetLogger().Log("Resource not found.", LogLevel.Error);
                return false;
            }

            return true;
        }
        
        public static bool TryGetString(Assembly ass, string resourceName, out string result)
        {
            if (GetStream(ass, resourceName, out var stream))
            {
                using var reader = new StreamReader(stream);

                result = reader.ReadToEnd();

                Logger.GetLogger().Log("Done.");

                return true;
            }
            else
            {
                result = string.Empty;
                return false;
            }
        }
    }
}