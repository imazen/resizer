using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Util {
    public class StreamUtils {

        public static MemoryStream CopyStream(Stream s) {
            return s.CopyToMemoryStream();
        }


        /// <summary>
        /// Copies a read stream to a write stream.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void CopyTo(Stream src, Stream dest) {
            src.CopyToStream(dest);
        }

        /// <summary>
        /// Copies the remaining portion of the specified stream to a byte array of exact size.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static byte[] CopyToBytes(Stream src) {
            return src.CopyToBytes();
        }
    }
}
