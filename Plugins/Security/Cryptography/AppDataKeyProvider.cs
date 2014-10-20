using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Hosting;
using System.IO;
using System.Xml;
using ImageResizer.Util;

namespace ImageResizer.Plugins.Security.Cryptography {
    internal class AppDataKeyProvider:IKeyProvider {

        private string _keyFilePath;
        /// <summary>
        /// The physical path to the keys file
        /// </summary>
        public string KeyFilePath {
            get {
                if (_keyFilePath == null) _keyFilePath = HostingEnvironment.MapPath("~/App_Data/encryption-keys.config");
                return _keyFilePath;
            }
        }

        private object syncLock = new object();

        private Dictionary<string, byte[]> keys = null;

        protected void New() {
            lock (syncLock) {
                keys = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            }
        }

        protected void Load() {
            if (!File.Exists(KeyFilePath)) {
                New();
                return;
            }
            var k = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            lock (syncLock) {
                var s = new XmlReaderSettings();
                s.ValidationType = ValidationType.None;
                s.CloseInput = true;
                using (XmlReader r = XmlReader.Create(KeyFilePath, s)) {
                    while (r.Read()) {
                        if (r.NodeType == XmlNodeType.Element) {
                            if (r.Name == "key") {
                                r.MoveToAttribute("name");
                                string name = r.Value;
                                r.MoveToContent();
                                k.Add(name, PathUtils.FromBase64UToBytes(r.Value));
                            }
                        }
                    }
                }
                keys = k;
            }
            
        }

        protected void Save() {
            lock (syncLock) {
                using (XmlWriter w = XmlWriter.Create(KeyFilePath, new XmlWriterSettings())) {
                    w.WriteStartDocument();
                    w.WriteStartElement("keys");
                    foreach (KeyValuePair<string, byte[]> p in keys) {
                        w.WriteStartElement("key");
                        w.WriteAttributeString("name", p.Key);
                        w.WriteValue(Util.PathUtils.ToBase64U(p.Value));
                        w.WriteEndElement();
                    }
                    w.WriteEndDocument();
                    w.Close();
                }
            }
        }

        public byte[] GetKey(string name, int sizeInBytes) {
            lock (syncLock) {
                if (keys == null) Load(); //Load if this is the first request

                string lookup = name + "_" + sizeInBytes;

                byte[] key;
                if (!keys.TryGetValue(lookup, out key)) {
                    //Generate and insert if missing
                    key = new byte[sizeInBytes];
                    new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(key);
                    keys[lookup] = key;
                    //Then save
                    Save();
                }
                return key;
            }
        }
    }
}
