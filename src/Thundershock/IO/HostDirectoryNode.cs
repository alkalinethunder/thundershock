using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Thundershock.IO
{
    public class HostDirectoryNode : Node
    {
        private string _directory;
        private Node _parent;

        public HostDirectoryNode(Node parent, string hostPath)
        {
            _parent = parent;
            _directory = hostPath;
        }

        public override void Delete(bool recursive)
        {
            Directory.Delete(_directory, recursive);
        }

        public override bool CanDelete => true;
        public override bool CanRead => false;
        public override bool CanWrite => false;
        public override bool CanExecute => false;
        public override bool CanList => true;
        public override bool CanCreate => true;
        public override Node Parent => _parent;

        public override Stream CreateFile(string name)
        {
            return File.Create(Path.Combine(_directory, name));
        }

        public override void CreateDirectory(string name)
        {
            Directory.CreateDirectory(Path.Combine(_directory, name));
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                foreach (var dir in Directory.GetDirectories(_directory, "*", new EnumerationOptions()))
                {
                    yield return new HostDirectoryNode(this, dir);
                }

                foreach (var file in Directory.GetFiles(_directory, "*", new EnumerationOptions()))
                {
                    yield return new HostFileNode(this, file);
                }
            }
        }
        public override string Name => Path.GetFileName(_directory);
    }
}