using Thundershock;

namespace VideoPlayer
{
    public sealed class VideoPlayerApp : NewGameAppBase
    {
        protected override void OnLoad()
        {
            base.OnLoad();
            LoadScene<VideoPlayerScene>();
        }
    }
}