using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ImageResizer.ReleaseBuilder {
    public class FsQuery {
        string[] files;
        string[] folders;
        public FsQuery(string dir) {
            files = Directory.GetFiles(dir, "", SearchOption.AllDirectories);
            folders = Directory.GetDirectories(dir, "", SearchOption.AllDirectories);
        }

        public List<Pattern> exclusions = new List<Pattern>();





    }
}
