using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BuildTools {
    /// <summary>
    /// File deletion utilities
    /// </summary>
    public class Futile {
        TextWriter err;
        public Futile(TextWriter errStream){
            this.err = errStream;
        }
        public void DelFiles(IEnumerable<string> paths) {
            foreach(string s in paths){
                try {
                    File.Delete(s);
                    err.WriteLine("Removed " + s);
                } catch (Exception ex) {
                    err.WriteLine("Failed to delete " + s + " " + ex.ToString());
                }
            }
        }
        public void DelFolders(IEnumerable<string> paths) {
            foreach (string s in paths) {
                try {
                    Directory.Delete(s, true);
                    err.WriteLine("Removed " + s);
                } catch (Exception ex) {
                    err.WriteLine("Failed to delete " + s + " " + ex.ToString());
                }
            }
        }
    }
}
