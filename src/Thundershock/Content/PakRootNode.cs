using System;
using System.Collections.Generic;
using Thundershock.IO;

namespace Thundershock.Content
{
    internal class PakRootNode : Node
    {
        private Dictionary<string, PakFile> _paks = new();

        public override bool CanDelete => false;
        public override bool CanRead => false;
        public override bool CanWrite => false;
        public override bool CanExecute => false;
        public override bool CanList => true;
        public override bool CanCreate => false;
        public override Node Parent => this;

        public override IEnumerable<Node> Children
        {
            get
            {
                foreach (var key in _paks.Keys)
                {
                    yield return new PakNode(this, key, _paks[key], _paks[key].RootDirectory);
                }
            }
        }

        public override string Name => "/";

        public void AddPak(string vfsName, PakFile pak)
        {
            if (string.IsNullOrWhiteSpace(vfsName))
                throw new InvalidOperationException("Pak mount name cannot be blank.");

            if (_paks.ContainsKey(vfsName))
                throw new InvalidOperationException("PAK mountpoint already in use.");

            if (pak == null)
                throw new ArgumentNullException(nameof(pak));
            
            _paks.Add(vfsName, pak);
        }
    }
}