using System;
using System.Collections.Generic;

namespace Thundershock.Core.Ecs
{
    public class Registry
    {
        internal class GroupData
        {
            public int HashCode;
            public SparseSet Entities;
            IComponentStore[] _componentStores;

            public GroupData(Registry registry, int hashCode, params IComponentStore[] components)
            {
                HashCode = hashCode;
                Entities = new SparseSet(registry._maxEntities);
                _componentStores = components;
            }

            internal void OnEntityAdded(uint entityId)
            {
                if (!Entities.Contains(entityId))
                {
                    foreach (var store in _componentStores)
                        if (!store.Contains(entityId)) return;
                    Entities.Add(entityId);
                }
            }

            internal void OnEntityRemoved(uint entityId)
            {
                if (Entities.Contains(entityId)) Entities.Remove(entityId);
            }
        }

        private readonly uint _maxEntities;
        private Dictionary<Type, IComponentStore> _data = new();
        private uint _nextEntity;
        private readonly List<GroupData> _groups = new();

        public Registry(uint maxEntities) => _maxEntities = maxEntities;

        public ComponentStore<T> Assure<T>()
        {
            var type = typeof(T);
            if (_data.TryGetValue(type, out _)) return (ComponentStore<T>)_data[type];

            var newStore = new ComponentStore<T>(_maxEntities);
            _data[type] = newStore;
            return newStore;
        }

        public Entity Create() => new Entity(_nextEntity++);

        public void Destroy(Entity entity)
        {
            foreach (var store in _data.Values)
                store.RemoveIfContains(entity.Id);
        }

        public void AddComponent<T>(Entity entity, T component) => Assure<T>().Add(entity, component);

        public ref T GetComponent<T>(Entity entity) => ref Assure<T>().Get(entity.Id);

        public bool HasComponent<T>(Entity entity)
        {
            var store = Assure<T>();
            return store.Contains(entity.Id);
        }
        
        public bool TryGetComponent<T>(Entity entity, ref T component)
        {
            var store = Assure<T>();
            if (store.Contains(entity.Id))
            {
                component = store.Get(entity.Id);
                return true;
            }

            return false;
        }

        public void RemoveComponent<T>(Entity entity) => Assure<T>().RemoveIfContains(entity.Id);

        public View<T> View<T>() => new View<T>(this);

        public View<T1, T2> View<T1, T2>() => new View<T1, T2>(this);

        public View<T1, T2, T3> View<T1, T2, T3>() => new View<T1, T2, T3>(this);

        public Group Group<T1, T2>()
        {
            var hash = HashCode.Combine(typeof(T1), typeof(T2));

            foreach (var group in _groups)
                if (group.HashCode == hash) return new Group(group);

            var groupData = new GroupData(this, hash, Assure<T1>(), Assure<T2>());
            _groups.Add(groupData);

            Assure<T1>().OnAdd += groupData.OnEntityAdded;
            Assure<T2>().OnAdd += groupData.OnEntityAdded;

            Assure<T1>().OnRemove += groupData.OnEntityRemoved;
            Assure<T2>().OnRemove += groupData.OnEntityRemoved;

            foreach (var entityId in View<T1, T2>()) groupData.Entities.Add(entityId);

            return new Group(groupData);
        }

        public Group Group<T1, T2, T3>()
        {
            var hash = HashCode.Combine(typeof(T1), typeof(T2), typeof(T3));

            foreach (var group in _groups)
                if (group.HashCode == hash) return new Group(group);

            var groupData = new GroupData(this, hash, Assure<T1>(), Assure<T2>(), Assure<T3>());
            _groups.Add(groupData);

            Assure<T1>().OnAdd += groupData.OnEntityAdded;
            Assure<T2>().OnAdd += groupData.OnEntityAdded;
            Assure<T3>().OnAdd += groupData.OnEntityAdded;

            Assure<T1>().OnRemove += groupData.OnEntityRemoved;
            Assure<T2>().OnRemove += groupData.OnEntityRemoved;
            Assure<T3>().OnRemove += groupData.OnEntityRemoved;

            foreach (var entityId in View<T1, T2, T3>()) groupData.Entities.Add(entityId);

            return new Group(groupData);
        }
    }}