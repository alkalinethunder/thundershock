using System;
using System.Collections.Generic;
using System.IO;

namespace Thundershock.IO
{
    public class HostFileNode : Node
    {
        private string _file;
        private Node _parent;

        public HostFileNode(Node parent, string file)
        {
            _file = file;
            _parent = parent;
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanExecute => false;
        public override bool CanList => false;
        public override bool CanCreate => false;
        public override Node Parent => _parent;
        public override IEnumerable<Node> Children => Array.Empty<Node>();
        public override string Name => Path.GetFileName(_file);
        public override bool CanDelete => true;
        public override long Length => new FileInfo(_file).Length;

        public override Stream Open(bool append)
        {
            return File.Open(_file, append ? FileMode.Append : FileMode.Open);
        }

        public override void Delete(bool recursive)
        {
            File.Delete(_file);
        }
    }
}