using System;

namespace Thundershock.Flumberboozles
{
    public class AttachedProperty<T> : IAttachedProperty
    {
        private string _name;
        private T _value;

        public string Name => _name;
        public T Value => _value;

        object IAttachedProperty.Value => _value;

        public void SetName(string name)
            => _name = name;
        
        public void SetValue(object value)
        {
            if (value == null)
            {
                _value = default;
            }
            else if (value is T t)
            {
                _value = t;
            }
            else
            {
                throw new InvalidCastException("Cannot set the value of this property to a value of type " +
                                               value.GetType().FullName + ".");
            }
        }
    }
}