using System.Collections.Generic;
using Thundershock.IO;

namespace Thundershock.Content
{
    public class PakNode : Node
    {
        private string _name;
        private PakFile _pak;
        private Node _parent;
        private PakDirectory _pakDirectory;

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
                    yield return new PakNode(this, child.Name, _pak, child);
                }
            }
        }
        public override string Name => _name;

        public PakNode(Node parent, string name, PakFile pak, PakDirectory directory)
        {
            _parent = parent;
            _name = name;
            _pak = pak;
            _pakDirectory = directory;
        }
    }
}