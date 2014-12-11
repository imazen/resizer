using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.IsolatedStorage;
using System.IO;

namespace BuildTools {
    public class CredentialStore {

        private IsolatedStorageFile isf;

        private string  filename = "credentials.txt";
        public CredentialStore() {
            isf = IsolatedStorageFile.GetUserStoreForAssembly();
        }

        public Dictionary<string, string> Credentials = new Dictionary<string, string>();
        public SortedList<string, string> NeededCredentials = new SortedList<string, string>();


        public void Load() {
            if (!isf.FileExists(filename)) return;
            using (var s = isf.OpenFile(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.None)) {
                using (var t = new StreamReader(s, Encoding.UTF8)) {
                    while (t.Peek() >= 0){
                        string line = t.ReadLine();
                        int ix = line.IndexOf(':');
                        if (ix >= 0){
                            Credentials[line.Substring(0,ix)] = line.Substring(ix + 1);
                        }
                    }
                }
            }
        }
        public void Save() {
            using (var s = isf.OpenFile(filename, FileMode.Create, FileAccess.Write, FileShare.None)) {
                using (var t = new StreamWriter(s, Encoding.UTF8)) {
                    foreach(var p in Credentials){
                        t.WriteLine(p.Key + ":" + p.Value);
                    }
                }
            }
        }

        public void Need(string key, string description) {
            NeededCredentials.Add(key, description);
        }

        public string Get(string key, string defaultValue) {
            if (Credentials.ContainsKey(key)) return Credentials[key];
            else return defaultValue;
        }

        public void AcquireCredentials() {
            Load();
            foreach(KeyValuePair<string,string> p in NeededCredentials){
                if (Credentials.ContainsKey(p.Key)) continue;
                var newVal = new Interaction().change(p.Value, null);
                if (newVal != null) Credentials[p.Key] = newVal;
            }
            Save();
        }
    }
}
