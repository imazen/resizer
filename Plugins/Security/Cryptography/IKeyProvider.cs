using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.Security.Cryptography {
    internal interface IKeyProvider {
        /// <summary>
        /// Locates or creates a cryptographic key of the given byte size and the given name. Implementations may store keys in RAM or on disk. 
        /// calling ("a", 8) and ("a",16) will generate and store two different keys. I.e, the size is part of the lookup key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sizeInBytes"></param>
        /// <returns></returns>
        byte[] GetKey(string name, int sizeInBytes);
    }
}
