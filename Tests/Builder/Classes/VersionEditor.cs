using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace ImageResizer.ReleaseBuilder {
    public class VersionEditor {
        string filename;
        string contents = null;
        public VersionEditor(string filename) {
            contents = File.ReadAllText(filename, UTF8Encoding.UTF8);
            this.filename = filename;
        }

        public string get(string name) {
            Regex r = new Regex("\\[assembly\\:\\s*" + Regex.Escape(name) + "\\s*\\(\\s*\"" + "(?<value>[^\"]*)" + "\"\\s*)\\s*\\]", RegexOptions.IgnoreCase);

            (r.
            [assembly: AssemblyVersion("
        }


        public void Save() {
            File.WriteAllText(filename, contents, UTF8Encoding.UTF8);
        }
    }
}
