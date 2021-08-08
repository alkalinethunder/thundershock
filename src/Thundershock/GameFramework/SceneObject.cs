using System;
using System.Diagnostics;
using Thundershock.Core.Ecs;

namespace Thundershock.GameFramework
{
    public class SceneObject
    {
        private WeakReference<Registry> _registry;
        private Entity _entity;
        private Scene _scene;
        
        public string Name
        {
            get => GetComponent<string>();
            set => SetComponent(value ?? Guid.NewGuid().ToString());
        }
        
        public SceneObject(Registry registry, Entity entity, Scene scene)
        {
            _scene = scene;
            _entity = entity;
            _registry = new WeakReference<Registry>(registry);
        }
        
        public bool HasComponent<T>()
        {
            if (_registry.TryGetTarget(out var reg))
            {
                return reg.HasComponent<T>(_entity);
            }

            return false;
        }

        public void AddComponent<T>(T component)
        {
            Debug.Assert(!HasComponent<T>(), "Entity already has component.");
            
            if (_registry.TryGetTarget(out var reg))
            {
                reg.AddComponent(_entity, component);
            }
        }

        public ref T GetComponent<T>()
        {
            if (_registry.TryGetTarget(out var reg))
            {
                return ref reg.GetComponent<T>(_entity);
            }

            throw new InvalidOperationException("Entity doesn't contain that component.");
        }

        public void SetComponent<T>(T value)
        {
            if (HasComponent<T>())
            {
                if (_registry.TryGetTarget(out var reg))
                    reg.GetComponent<T>(_entity) = value;
            }
            else
            {
                AddComponent(value);
            }
        }
    }
}