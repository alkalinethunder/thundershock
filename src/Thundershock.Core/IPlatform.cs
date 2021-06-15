using Thundershock.Core.Debugging;
using Thundershock.Debugging;

namespace Thundershock.Core
{
    public interface IPlatform
    {
        void Initialize(Logger logger);
    }
}