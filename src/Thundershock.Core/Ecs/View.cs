using System;
using System.Collections;
using System.Collections.Generic;

namespace Thundershock.Core.Ecs
{
    public struct View<T> : IEnumerable<uint>
    {
        Registry _registry;

        public View(Registry registry) => _registry = registry;

        public IEnumerator<uint> GetEnumerator() => _registry.Assure<T>().Set.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct View<T1, T2> : IEnumerable<uint>
    {
        struct Enumerator : IEnumerator<uint>
        {
            private IComponentStore _store;
            private IEnumerator<uint> _setEnumerator;

            public Enumerator(Registry registry)
            {
                var store1 = registry.Assure<T1>();
                var store2 = registry.Assure<T2>();

                if (store1.Count > store2.Count)
                {
                    _setEnumerator = store2.Entities.GetEnumerator();
                    _store = store1;
                }
                else
                {
                    _setEnumerator = store1.Entities.GetEnumerator();
                    _store = store2;
                }
            }

            public uint Current => _setEnumerator.Current;

            object IEnumerator.Current => _setEnumerator.Current;

            public void Dispose()
            {}

            public bool MoveNext()
            {
                while (_setEnumerator.MoveNext())
                {
                    var entityId = _setEnumerator.Current;
                    if (!_store.Contains(entityId)) continue;
                    return true;
                }
                return false;
            }

            public void Reset() => _setEnumerator.Reset();
        }

        Registry _registry;

        public View(Registry registry) => _registry = registry;

        public IEnumerator<uint> GetEnumerator() => new Enumerator(_registry);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    // Would U kindly move out of the way? You're blocking the T V.
    public struct View<T1, T2, T3> : IEnumerable<uint>
    {
        struct Enumerator : IEnumerator<uint>
        {
            
            // ReSharper disable once StaticMemberInGenericType
            private static readonly IComponentStore[] Sorter = new IComponentStore[3];

            private IComponentStore _store1;
            private IComponentStore _store2;
            private IEnumerator<uint> _setEnumerator;

            public Enumerator(Registry registry)
            {
                Sorter[0] = registry.Assure<T1>();
                Sorter[1] = registry.Assure<T2>();
                Sorter[2] = registry.Assure<T3>();
                Array.Sort(Sorter, (first, second) => first.Entities.Count.CompareTo(second.Entities.Count));

                _setEnumerator = Sorter[0].Entities.GetEnumerator();
                _store1 = Sorter[1];
                _store2 = Sorter[2];
            }

            public uint Current => _setEnumerator.Current;

            object IEnumerator.Current => _setEnumerator.Current;

            public void Dispose()
            {}

            public bool MoveNext()
            {
                while (_setEnumerator.MoveNext())
                {
                    var entityId = _setEnumerator.Current;
                    if (!_store1.Contains(entityId) || !_store2.Contains(entityId)) continue;
                    return true;
                }
                return false;
            }

            public void Reset() => _setEnumerator.Reset();
        }


        Registry _registry;

        public View(Registry registry) => _registry = registry;

        public IEnumerator<uint> GetEnumerator() => new Enumerator(_registry);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }}