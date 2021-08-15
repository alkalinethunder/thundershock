using Thundershock.Core;

namespace Thundershock
{
    /// <summary>
    /// Provides functionality for creating a game system.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// Initializes the system for the first time.
        /// </summary>
        /// <param name="scene">An instance of the scene that created this system.</param>
        void Init(Scene scene);
        
        /// <summary>
        /// Unloads the system.
        /// </summary>
        void Unload();
        
        /// <summary>
        /// Triggers when the level has fully loaded.
        /// </summary>
        void Load();
        
        /// <summary>
        /// Called every time the scene updates.
        /// </summary>
        /// <param name="gameTime">The time since the last scene update.</param>
        void Update(GameTime gameTime);
        
        /// <summary>
        /// Called when it's time for the system to render.
        /// </summary>
        /// <param name="gameTime">The time since the last scene update.</param>
        void Render(GameTime gameTime);
    }
}