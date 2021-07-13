using System.Collections;
using System.Collections.Generic;

namespace Thundershock.Core.Ecs
{
    public struct Group : IEnumerable<uint>
    {
        private Registry.GroupData _groupData;

        internal Group(Registry.GroupData groupData)
        {
            _groupData = groupData;
        }

        public IEnumerator<uint> GetEnumerator() => _groupData.Entities.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }}