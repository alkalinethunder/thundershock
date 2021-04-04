using System.Collections.Generic;
using System.Linq;

namespace Thundershock.Flumberboozles
{
    public class PropertySet
    {
        private List<IAttachedProperty> _properties = new();

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
        }

        public void SetValue<T>(string name, T value)
        {
            if (ContainsKey(name))
                RemoveValue(name);

            var prop = new AttachedProperty<T>();

            prop.SetName(name);
            prop.SetValue(value);

            _properties.Add(prop);
        }
    }
}