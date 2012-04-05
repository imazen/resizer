using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ImageResizer.Util {
    public class StreamUtils {

        public static MemoryStream CopyStream(Stream s) {
            MemoryStream ms = new MemoryStream(s.CanSeek ? ((int)s.Length + 8) : 4096);
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
        /// <summary>
        /// Copies the remaining portion of the specified stream to a byte array of exact size.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static byte[] CopyToBytes(Stream src) {
            byte[] bytes;
            if (src is MemoryStream) {
                //Slice from 
                MemoryStream ms = src as MemoryStream;
                try{
                    byte[] buffer = ms.GetBuffer();
                    long count =src.Length - src.Position;
                    bytes = new byte[src.Length - src.Position];
                    Array.Copy(buffer, src.Position, bytes, 0, count);
                    return bytes;
                }catch(UnauthorizedAccessException) //If we can't slice it, then we read it like a normal stream
                {}
            }
            
            if (src.CanSeek) {
                // Read the source file into a byte array.
                int numBytesToRead = (int)(src.Length - src.Position);
                bytes = new byte[numBytesToRead];
                int numBytesRead = 0;
                while (numBytesToRead > 0) {
                    // Read may return anything from 0 to numBytesToRead.
                    int n = src.Read(bytes, numBytesRead, numBytesToRead);

                    // Break when the end of the file is reached.
                    if (n == 0)
                        break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }
                Debug.Assert(numBytesRead == bytes.Length);
                return bytes;
            } else {
                //No seeking, so we have to buffer to an intermediate memory stream
                var ms = new MemoryStream();
                CopyTo(src,(Stream) ms);
                ms.Seek(0, SeekOrigin.Begin);
                return CopyToBytes(ms);
            }
        }
    }
}
