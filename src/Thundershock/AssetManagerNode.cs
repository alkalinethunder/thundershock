using System;
using System.Collections.Generic;
using System.IO;
using Thundershock.Content;
using Thundershock.IO;

namespace Thundershock
{
    public class AssetManagerNode : Node
    {
        private Dictionary<string, string> _directoryMounts = new();
        private Dictionary<string, PakFile> _pakFiles = new();
        
        public override bool CanDelete => false;
        public override bool CanRead => false;
        public override bool CanWrite => false;
        public override bool CanExecute => false;
        public override bool CanList => true;
        public override bool CanCreate => false;
        public override Node Parent => null;

        public override IEnumerable<Node> Children
        {
            get
            {
                foreach (var mount in _directoryMounts.Keys)
                {
                    var path = _directoryMounts[mount];
                    var node = new HostDirectoryNode(this, path, mount);
                    yield return node;
                }
            }
        }
        public override string Name => "/";

        public void AddDirectory(string mountName, string path)
        {
            if (string.IsNullOrWhiteSpace(mountName))
                throw new InvalidOperationException("Mount name is required.");
            
            if (_pakFiles.ContainsKey(mountName) || _pakFiles.ContainsKey(mountName))
                throw new InvalidOperationException("Mount name already used.");

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException(path);
            
            _directoryMounts.Add(mountName, path);
        }
    }
}