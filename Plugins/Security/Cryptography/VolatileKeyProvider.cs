// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace ImageResizer.Plugins.Security.Cryptography
{
    internal class VolatileKeyProvider : IKeyProvider
    {
        private object syncLock = new object { };

        private Dictionary<string, byte[]> keys = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);


        public byte[] GetKey(string name, int sizeInBytes)
        {
            name += "_" + sizeInBytes;
            lock (syncLock)
            {
                byte[] val;
                if (!keys.TryGetValue(name, out val))
                {
                    val = new byte[sizeInBytes];
                    new RNGCryptoServiceProvider().GetBytes(val);
                    keys[name] = val;
                }

                return val;
            }
        }
    }
}