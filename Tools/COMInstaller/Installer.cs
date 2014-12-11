using System;
using System.Collections.Generic;
using System.Text;

namespace COMInstaller {
    public class Installer {

        public Installer(DllCollection newFiles, string destFolder) {
            this.newFiles = newFiles;
            this.destFolder = destFolder;
        }

        public DllCollection newFiles;
        public string destFolder;

        public string Install() {
            StringBuilder log = new StringBuilder();
            foreach (Dll d in newFiles.Values) {
                log.AppendLine("Copying and registering " + d.path);
                d.CopyAllTo(destFolder);
                //Register the ones that start with ImageResizer
                if (d.NameWithoutExtension.StartsWith("ImageResizer")) log.AppendLine(d.Register());
            }
            return log.ToString();
        }
    }
}
