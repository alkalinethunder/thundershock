using System.IO;
using System.Reflection;
using Thundershock.Core.Debugging;
using Thundershock.Debugging;

namespace Thundershock.Core
{
    public static class Resource
    {
        public static bool TryGetString(Assembly ass, string resourceName, out string result)
        {
            result = string.Empty;
            
            if (ass == null)
            {
                Logger.GetLogger().Log($"Tried to get an embedded resource from an invalid assembly.", LogLevel.Error);
                return false;
            }

            Logger.GetLogger()
                .Log($"Retrieving embedded resource " + resourceName + " from assembly " + ass.ToString());
            
            using var stream = ass.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                Logger.GetLogger().Log("Resource not found.", LogLevel.Error);
                return false;
            }

            using var reader = new StreamReader(stream);

            result = reader.ReadToEnd();

            Logger.GetLogger().Log("Done.");

            return true;
        }
    }
}