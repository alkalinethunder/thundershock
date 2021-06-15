namespace Thundershock.Core
{
    public interface IGlobalComponent
    {
        AppBase App { get; }

        void Initialize(AppBase app);

        void Unload();
        void Update(GameTime gameTime);
    }
}