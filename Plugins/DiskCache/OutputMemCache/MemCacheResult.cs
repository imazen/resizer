using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ImageResizer.Plugins.MemCache {

    public class MemCacheResult  {

        public MemCacheResult( byte[] data) {
            this.data = data;
        }
        byte[] data;

        public byte[] Data {
            get { return data; }
        }

        public long BytesOccupied { get { return Data.Length + 32; } }


    }
}
