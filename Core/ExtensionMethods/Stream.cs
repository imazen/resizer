using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ImageResizer.Util;

namespace ImageResizer.ExtensionMethods {
    public static class StreamExtensions {

        public static MemoryStream CopyToMemoryStream(this Stream s) {
            return StreamUtils.CopyStream(s, false);
        }

        public static MemoryStream CopyToMemoryStream(this Stream s, bool entireStream) {
            return StreamUtils.CopyStream(s, false);
        }

        public static byte[] CopyToBytes(this Stream s) {
            return StreamUtils.CopyToBytes(s, false);
        }

        public static byte[] CopyToBytes(this Stream s, bool entireStream) {
            return StreamUtils.CopyToBytes(s, entireStream);
        }

        public static void CopyToStream(this Stream s, Stream other) {
            StreamUtils.CopyTo(s, other, false);
        }

        public static void CopyToStream(this Stream s, Stream other, bool entireStream) {
            StreamUtils.CopyTo(s, other, entireStream);
        }


    }
}
