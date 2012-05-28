using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ImageResizer.Plugins.DiskCache.Async {
    public class AsyncWrite {

        public AsyncWrite(AsyncWriteCollection parent, MemoryStream data, string physicalPath, string relativePath, DateTime modifiedDateUtc) {
            this._parent = parent;
            this._data = data;
            this._physicalPath = physicalPath;
            this._relativePath = relativePath;
            this._modifiedDateUtc = modifiedDateUtc;
            this._jobCreatedAt = DateTime.UtcNow;
        }

        private AsyncWriteCollection _parent = null;

        public AsyncWriteCollection Parent {
            get { return _parent; }
        }

        private MemoryStream _data;

        private string _physicalPath = null;

        public string PhysicalPath {
            get { return _physicalPath; }
        }

        private string _relativePath = null;

        public string RelativePath {
            get { return _relativePath; }
        }

        private DateTime _modifiedDateUtc = DateTime.MinValue;

        public DateTime ModifiedDateUtc {
            get { return _modifiedDateUtc; }
        }

        private DateTime _jobCreatedAt;
        /// <summary>
        /// Returns the UTC time this AsyncWrite object was created.
        /// </summary>
        public DateTime JobCreatedAt {
            get { return _jobCreatedAt; }
        }

        public long GetDataLength() {
            return _data.Length;
        }
        public long GetBufferLength() {
            return _data.Capacity;
        }

        /// <summary>
        /// Wraps the data in a readonly MemoryStream so it can be accessed on another thread
        /// </summary>
        /// <returns></returns>
        public MemoryStream GetReadonlyStream() {
            //Wrap the original buffer in a new MemoryStream.
            return new MemoryStream(_data.GetBuffer(), 0, (int)_data.Length, false, true);
        }
    }
}
