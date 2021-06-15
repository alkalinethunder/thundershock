using System.Collections.Generic;

namespace Thundershock.Content
{
    public class PakDirectory
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public PakDirectoryType DirectoryType { get; set; }
        public List<PakDirectory> Children { get; set; } = new();
        public long DataStart { get; set; }
        public long DataLength { get; set; }
    }
}