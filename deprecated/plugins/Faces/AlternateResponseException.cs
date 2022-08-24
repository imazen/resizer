// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.Faces {
    public class AlternateResponseException:Exception {
        public byte[] ResponseData { get; set; }
        public string ContentType { get; set; }
        public int StatusCode { get; set; }
        public AlternateResponseException(string message) : base(message) { }

    }
}
