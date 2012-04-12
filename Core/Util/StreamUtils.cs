using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ImageResizer.Util {
    public class StreamUtils {

        public static MemoryStream CopyStream(Stream s, bool entireStream = false, int chunkSize = 0x4000) {
            MemoryStream ms = new MemoryStream(s.CanSeek ? ((int)s.Length + 8 - (entireStream ? 0 : (int)s.Position)) : chunkSize);
            CopyTo(s, ms, entireStream, chunkSize);
            ms.Position = 0;
            return ms;
        }


        /// <summary>
        /// Copies a read stream to a write stream.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void CopyTo(Stream src, Stream dest, bool entireStream = false, int chunkSize = 0x4000) {
            if (src is MemoryStream && src.CanSeek) {
                try {
                    int pos = entireStream ? 0 : (int)src.Position;
                    dest.Write(((MemoryStream)src).GetBuffer(), pos, (int)(src.Length - pos));
                    return;
                }catch(UnauthorizedAccessException) //If we can't slice it, then we read it like a normal stream
                {}
            }
            if (dest is MemoryStream && src.CanSeek) {
                try {
                    int srcPos = entireStream ? 0 : (int)src.Position;
                    int pos = (int)dest.Position;
                    int length = (int)(src.Length - srcPos) + pos;
                    dest.SetLength(length);

                    var data = ((MemoryStream)dest).GetBuffer();
                    while (pos < length) {
                        pos += src.Read(data, pos, length - pos);
                    }
                    return;
                }catch(UnauthorizedAccessException) //If we can't write directly, fall back
                {}
            }
            int size = (src.CanSeek) ? Math.Min((int)(src.Length - (entireStream ? 0 : (int)src.Position)), 0x2000) : 0x2000;
            byte[] buffer = new byte[size];
            int n;
            do {
                n = src.Read(buffer, 0, buffer.Length);
                dest.Write(buffer, 0, n);
            } while (n != 0);
        }

        /// <summary>
        /// Copies the remaining portion of the specified stream to a byte array of exact size.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static byte[] CopyToBytes(Stream src, bool entireStream = false, int chunkSize = 0x4000) {
            byte[] bytes;
            if (src is MemoryStream) {
                //Slice from 
                MemoryStream ms = src as MemoryStream;
                try{
                    byte[] buffer = ms.GetBuffer();
                    long pos = entireStream ? 0 : src.Position;
                    long count =src.Length - pos;
                    bytes = new byte[count];
                    Array.Copy(buffer, pos, bytes, 0, count);
                    return bytes;
                }catch(UnauthorizedAccessException) //If we can't slice it, then we read it like a normal stream
                {}
            }
            
            if (src.CanSeek) {
                long pos = entireStream ? 0 : src.Position;
                // Read the source file into a byte array.
                int numBytesToRead = (int)(src.Length - pos);
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
                CopyTo(src, ms,  entireStream,  chunkSize);
                ms.Seek(0, SeekOrigin.Begin);
                return CopyToBytes(ms,  entireStream,  chunkSize);
            }
        }
    }
}
