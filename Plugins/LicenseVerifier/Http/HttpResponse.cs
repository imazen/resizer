// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
using System.Collections.Generic;
using System.Net;

namespace ImageResizer.Plugins.LicenseVerifier.Http {
    public sealed class HttpResponse {
        public Uri ResponseUri { get; set; }
        public IList<HttpHeader> Headers { get; private set; }
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public string ContentType { get; set; }
        public long ContentLength { get; set; }
        public string ContentEncoding { get; set; }
        public string Content { get; set; }
        public Exception Error { get; set; }

        public HttpResponse() {
            Headers = new List<HttpHeader>();
        }
    }
}
