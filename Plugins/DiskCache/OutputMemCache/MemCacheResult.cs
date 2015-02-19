// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
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
