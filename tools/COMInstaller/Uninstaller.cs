using System;
using System.Collections.Generic;
using System.Text;

namespace COMInstaller {
    public class Uninstaller {


        public Uninstaller( DllCollection oldFiles){
            this.oldFiles = oldFiles;
        }
        public DllCollection oldFiles;
        
        public string Uninstall(){
            StringBuilder log = new StringBuilder();
            //Gotta unregister all before deleting any
            foreach(Dll d in oldFiles.Values){
                if (!d.NameWithoutExtension.StartsWith("ImageResizer")) continue;
                log.AppendLine("Unregistering " + d.path);
                log.AppendLine(d.Unregister());
            }
            foreach (Dll d in oldFiles.Values) {
                log.AppendLine("Removing " + d.path);
                d.DeleteAll();
            }
            return log.ToString();
        }
    }
}
