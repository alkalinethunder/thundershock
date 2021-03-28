using System.Collections.Generic;
using System.IO;

namespace Thundershock.IO
{
    public class WindowsMasterNode : Node
    {
        public override bool CanDelete => false;
        public override bool CanRead => false;
        public override bool CanWrite => false;
        public override bool CanExecute => false;
        public override bool CanList => true;
        public override bool CanCreate => false;
        public override Node Parent => null;
        public override string Name => string.Empty;

        public override IEnumerable<Node> Children
        {
            get
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    yield return new WindowsDriveNode(this, drive);
                }
            }
        }
    }
}