using System;
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
