using System;
using System.Collections;
using System.Collections.Generic;

namespace Thundershock.Core.Ecs
{
    public class SparseSet : IEnumerable<uint>
    {
        struct Enumerator : IEnumerator<uint>
        {
            private uint[] _dense;
            private uint _size;
            private uint _current;
            private uint _next;

            public Enumerator(uint[] dense, uint size)
            {
                _dense = dense;
                _size = size;
                _current = 0;
                _next = 0;
            }

            public uint Current => _current;

            object IEnumerator.Current => _current;

            public void Dispose()
            {}

            public bool MoveNext()
            {
                if (_next < _size)
                {
                    _current = _dense[_next];
                    _next++;
                    return true;
                }

                return false;
            }

            public void Reset() => _next = 0;
        }


        private readonly uint _max;
        private uint _size;
        private readonly uint[] _dense;
        private readonly uint[] _sparse;

        private uint Size => _size;
        
        public uint Count => _size;

        public SparseSet(uint maxValue)
        {
            _max = maxValue + 1;
            _size = 0;
            _dense = new uint[_max];
            _sparse = new uint[_max];
        }

        public void Add(uint value)
        {
            if (value < _max && !Contains(value))
            {
                _dense[_size] = value;
                _sparse[value] = _size;
                _size++;
            }
        }

        public void Remove(uint value)
        {
            if (Contains(value))
            {
                _dense[_sparse[value]] = _dense[_size - 1];
                _sparse[_dense[_size - 1]] = _sparse[value];
                _size--;
            }
        }

        public uint Index(uint value) => _sparse[value];

        public bool Contains(uint value)
        {
            if (value >= _max)
                return false;
            else
                return _sparse[value] < _size && _dense[_sparse[value]] == value;
        }

        public void Clear() => _size = 0;

        public IEnumerator<uint> GetEnumerator() => new Enumerator(_dense, _size);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override bool Equals(object obj) => throw new Exception("Why are you comparing SparseSets?");

        public override int GetHashCode() => HashCode.Combine(_max, Size, _dense, _sparse, Count);
    }}