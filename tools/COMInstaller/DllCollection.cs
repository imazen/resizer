using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace COMInstaller {
    public class DllCollection:Dictionary<string,Dll>{
        /// <summary>
        /// Builds a dictionary of dll names to Dll references. Searches the specified directories in order - elements are not overwritten.
        /// </summary>
        /// <param name="dirs"></param>
        public DllCollection(string[] dirs):base(StringComparer.OrdinalIgnoreCase) {
            foreach (string s in dirs) {
                if (!Directory.Exists(s)) continue;
                string[] files = Directory.GetFiles(s, "*.dll");
                foreach (string f in files) {
                    string key = Path.GetFileNameWithoutExtension(f);
                    if (!this.ContainsKey(key)) this[key] =  new Dll(f);
                }
            }
        }


    }
}
