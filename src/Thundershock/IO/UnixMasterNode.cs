using System.Collections.Generic;
using System.IO;

namespace Thundershock.IO
{
    public class UnixMasterNode : Node
    {
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
                var root = Name;
                foreach (var dir in Directory.GetDirectories(root, "*", new EnumerationOptions()))
                {
                    yield return new HostDirectoryNode(this, dir);
                }

                foreach (var file in Directory.GetFiles(root))
                {
                    yield return new HostFileNode(this, file);
                }

            }
        }
        public override string Name => "/";
            
        public override void CreateDirectory(string name)
        {
            Directory.CreateDirectory(Path.Combine(this.Name, name));
        }
        
        public override Stream CreateFile(string name)
        {
            return File.Create(Path.Combine(this.Name, name));
        }
    }
}