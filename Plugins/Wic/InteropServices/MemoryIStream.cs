using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;

namespace ImageResizer.Plugins.Wic.InteropServices {
    public class MemoryIStream : MemoryStream, IStream {
        public MemoryIStream() { }
        public MemoryIStream(byte[] buffer) : base(buffer) { }
        public MemoryIStream(int capacity) : base(capacity) { }
        public MemoryIStream(byte[] buffer, bool writable) : base(buffer, writable) { }
        public MemoryIStream(byte[] buffer, int index, int count) : base(buffer, index, count) { }
        public MemoryIStream(byte[] buffer, int index, int count, bool writable) : base(buffer, index, count, writable) { }
        public MemoryIStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) : base(buffer, index, count, writable, publiclyVisible) { }

        void IStream.Read(byte[] pv, int cb, IntPtr pcbRead) {
            Marshal.WriteInt64(pcbRead, Read(pv, 0, cb));
        }

        void IStream.Write(byte[] pv, int cb, IntPtr pcbWritten) {
            Write(pv, 0, cb);
            Marshal.WriteInt64(pcbWritten, cb);
        }

        void IStream.Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition) {
            long pos = Seek(dlibMove, dwOrigin == 0 ? SeekOrigin.Begin : dwOrigin == 1 ? SeekOrigin.Current : SeekOrigin.End);
            if (plibNewPosition != IntPtr.Zero) Marshal.WriteInt64(plibNewPosition, pos);
        }

        void IStream.SetSize(long libNewSize) {
            SetLength(libNewSize);
        }

        void IStream.CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten) {
            var bytes = new byte[cb];
            Marshal.WriteInt64(pcbRead, Read(bytes, 0, (int)cb));
            Marshal.WriteInt64(pcbWritten, cb);
            Write(bytes, 0, (int)cb);
        }

        void IStream.Commit(int grfCommitFlags) {
            Flush();
        }

        void IStream.Revert() {
        }

        void IStream.LockRegion(long libOffset, long cb, int dwLockType) {
        }

        void IStream.UnlockRegion(long libOffset, long cb, int dwLockType) {
        }

        void IStream.Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag) {
            pstatstg = new System.Runtime.InteropServices.ComTypes.STATSTG {type = 2};
        }

        void IStream.Clone(out IStream ppstm) {
            ppstm = (IStream)MemberwiseClone();
        }
    }
}