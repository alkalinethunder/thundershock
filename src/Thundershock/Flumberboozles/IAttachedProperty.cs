namespace Thundershock.Flumberboozles
{
    public interface IAttachedProperty
    {
        string Name { get; }
        object Value { get; }

        void SetName(string name);
        void SetValue(object value);
    }
}