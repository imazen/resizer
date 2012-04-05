using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Cryptography {
    public interface IKeyProvider {

         byte[] GetKey(string name, int sizeInBytes);
    }
}
