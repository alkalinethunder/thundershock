namespace Thundershock.Core.Input
{
    public class KeyCharEventArgs : KeyEventArgs
    {
        public char Character { get; }

        public KeyCharEventArgs(Keys key, char character) : base(key)
        {
            this.Character = character;
        }
    }
}