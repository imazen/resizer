using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BuildTools {
    public class RestorePoint:IDisposable {

        public RestorePoint(IEnumerable<string> paths) {
            foreach (string s in paths) {
                files.Add(s,new KeyValuePair<byte[],DateTime>( System.IO.File.ReadAllBytes(s), File.GetLastWriteTimeUtc(s)));
            }
        }
        public IEnumerable<string> Paths {
            get {
                return files.Keys;
           }
        }

        private Dictionary<string, KeyValuePair<byte[], DateTime>> files = new Dictionary<string, KeyValuePair<byte[], DateTime>>();
        public void Dispose() {
            foreach (string s in files.Keys) {
                byte[] newData = File.ReadAllBytes(s);
                if (ArraysMatch(newData, files[s].Key)) continue; //No changes were made.
                File.WriteAllBytes(s, files[s].Key);
                File.SetLastWriteTimeUtc(s, files[s].Value);
            }
        }

        private bool ArraysMatch(byte[] a, byte[] b) {
            if (b.Length != a.Length) return false;
            for (int i = 0; i < a.Length; i++) {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
