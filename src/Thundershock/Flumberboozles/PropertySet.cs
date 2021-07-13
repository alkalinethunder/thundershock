using System;
using System.Collections.Generic;
using System.Linq;

namespace Thundershock.Flumberboozles
{
    public class PropertySet
    {
        private List<IAttachedProperty> _properties = new();
        private IPropertySetOwner _owner;

        public PropertySet(IPropertySetOwner owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public T GetValue<T>()
        {
            return GetValue<T>(typeof(T).FullName);
        }

        public void SetValue<T>(T value)
        {
            SetValue(typeof(T).FullName, value);
        }
        
        public T GetValue<T>(string name)
        {
            var prop = _properties.OfType<AttachedProperty<T>>().FirstOrDefault(x => x.Name == name);

            if (prop != null)
                return prop.Value;

            return default;
        }

        public bool ContainsKey(string name)
        {
            return _properties.Any(x => x.Name == name);
        }

        public void RemoveValue(string name)
        {
            var all = _properties.Where(x => x.Name == name).ToArray();

            foreach (var item in all)
                _properties.Remove(item);
            
            _owner.NotifyPropertyModified(name);
        }

        public void SetValue<T>(string name, T value)
        {
            if (ContainsKey(name))
                RemoveValue(name);

            var prop = new AttachedProperty<T>();

            prop.SetName(name);
            prop.SetValue(value);

            _properties.Add(prop);
            
            _owner.NotifyPropertyModified(name);
        }
    }

    public interface IPropertySetOwner
    {
        void NotifyPropertyModified(string name);
    }
}