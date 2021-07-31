using System;
using System.Collections.Generic;
using System.IO;
using Thundershock.IO;

namespace Thundershock.Content
{
    public class PakNode : Node
    {
        private string _name;
        private PakFile _pak;
        private Node _parent;
        private PakDirectory _pakDirectory;
        private bool _useExtension;
        
        public override bool CanDelete => false;
        public override bool CanRead => _pakDirectory.DirectoryType == PakDirectoryType.File;
        public override bool CanWrite => false;
        public override bool CanExecute => false;
        public override bool CanList => _pakDirectory.DirectoryType != PakDirectoryType.File;
        public override bool CanCreate => false;
        public override Node Parent => _parent;

        public override IEnumerable<Node> Children
        {
            get
            {
                foreach (var child in _pakDirectory.Children)
                {
                    yield return new PakNode(this, child.Name, _pak, child, _useExtension);
                }
            }
        }

        public override string Name => _name;

        public PakNode(Node parent, string name, PakFile pak, PakDirectory directory, bool useExtensions = false)
        {
            _useExtension = useExtensions;
            _parent = parent;
            _pak = pak;
            _pakDirectory = directory;

            if (_useExtension)
            {
                _name = _pakDirectory.FileName ?? _pakDirectory.Name;
            }
            else
            {
                _name = name;
            }
        }

        public override Stream Open(bool append)
        {
            if (append)
                throw new InvalidOperationException("Read-only file. PAK data cannot be written to directly.");

            return _pak.LoadData(_pakDirectory);
        }
    }
}