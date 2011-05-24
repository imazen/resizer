using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ImageResizer.Util {
    public class StreamUtils {

        public static MemoryStream CopyStream(Stream s) {
            MemoryStream ms = new MemoryStream((int)s.Length + 8);
            CopyTo(s, ms);
            ms.Position = 0;
            return ms;
        }


        /// <summary>
        /// Copies a read stream to a write stream.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>


        public static void CopyTo(Stream src, Stream dest) {
            int size = (src.CanSeek) ? Math.Min((int)(src.Length - src.Position), 0x2000) : 0x2000;
            byte[] buffer = new byte[size];
            int n;
            do {
                n = src.Read(buffer, 0, buffer.Length);
                dest.Write(buffer, 0, n);
            } while (n != 0);
        }

        public static void CopyTo(MemoryStream src, Stream dest) {
            dest.Write(src.GetBuffer(), (int)src.Position, (int)(src.Length - src.Position));
        }

        public static void CopyTo(Stream src, MemoryStream dest) {
            if (src.CanSeek) {
                int pos = (int)dest.Position;
                int length = (int)(src.Length - src.Position) + pos;
                dest.SetLength(length);

                while (pos < length)
                    pos += src.Read(dest.GetBuffer(), pos, length - pos);
            } else
                CopyTo(src,(Stream)dest);
        }

    }
}
