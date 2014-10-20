using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.Security.Cryptography {
    internal class VolatileKeyProvider: IKeyProvider {

        private object syncLock = new object{};

        private Dictionary<string, byte[]> keys = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);


        public byte[] GetKey(string name, int sizeInBytes) {
            name += "_" + sizeInBytes;
            lock (syncLock) {
                byte[] val;
                if (!keys.TryGetValue(name, out val)){
                    val = new byte[sizeInBytes];
                    new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(val);
                    keys[name] = val;
                }
                return val;
            }
        }
    }
}
