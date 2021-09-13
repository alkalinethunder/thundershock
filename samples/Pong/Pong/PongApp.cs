using Thundershock;

namespace Pong
{
    public sealed class PongApp : NewGameAppBase
    {
        protected override void OnLoad()
        {
            LoadScene<PongScene>();
        }
    }
}