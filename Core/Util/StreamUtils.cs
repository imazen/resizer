using System;
using System.IO;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Util {
    [Obsolete("Use ImageResizer.ExtensionMethods.StreamExtensions instead")]
    public class StreamUtils {

        public static MemoryStream CopyStream(Stream s) {
            return StreamExtensions.CopyToMemoryStream(s);
        }


        /// <summary>
        /// Copies a read stream to a write stream.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void CopyTo(Stream src, Stream dest) {
            StreamExtensions.CopyToStream(src, dest);
        }

        /// <summary>
        /// Copies the remaining portion of the specified stream to a byte array of exact size.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static byte[] CopyToBytes(Stream src) {
            return StreamExtensions.CopyToBytes(src);
        }
    }
}
