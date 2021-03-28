using System;
using System.Collections.Generic;
using System.IO;

namespace Thundershock.IO
{
    public class NullNode : Node
    {
        private Node _parent;

        public NullNode(Node parent)
        {
            _parent = parent;
        }

        public override bool CanDelete => false;
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanExecute => false;
        public override bool CanList => false;
        public override bool CanCreate => false;
        public override Node Parent => _parent;
        public override IEnumerable<Node> Children => Array.Empty<Node>();
        public override string Name => "null";

        public override Stream Open(bool append)
        {
            return Stream.Null;
        }
    }
}