using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Resizing {
    public class ResizingCanceledException:Exception {
        public byte[] Data { get; set; }
        public string ContentType { get; set; }
        public int StatusCode { get; set; }
        public ResizingCanceledException(string message) : base(message) { }

    }
}
