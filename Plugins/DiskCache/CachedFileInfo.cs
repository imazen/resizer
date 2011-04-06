using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ImageResizer.Plugins.DiskCache {

    public class CachedFileInfo {
        public CachedFileInfo(FileInfo f) {
            modifiedUtc = f.LastWriteTimeUtc;
            accessedUtc = f.LastAccessTimeUtc;
            updatedUtc = f.CreationTimeUtc;
        }

        public CachedFileInfo(DateTime modifiedDate, DateTime createdDate) {
            this.modifiedUtc = modifiedDate;
            this.updatedUtc = createdDate;
            this.accessedUtc = createdDate;
        }
        public CachedFileInfo(DateTime modifiedDate, DateTime createdDate, DateTime accessedDate) {
            this.modifiedUtc = modifiedDate;
            this.updatedUtc = createdDate;
            this.accessedUtc = accessedDate;
        }
        public CachedFileInfo(CachedFileInfo f, DateTime accessedDate) {
            this.modifiedUtc = f.modifiedUtc;
            this.updatedUtc = f.updatedUtc;
            this.accessedUtc = accessedDate;
        }
        private volatile DateTime modifiedUtc = DateTime.MinValue;
        /// <summary>
        /// The modified date of the source file that the cached file is based on.
        /// </summary>
        public DateTime ModifiedUtc {
            get { return modifiedUtc; }
        }
        private volatile DateTime accessedUtc = DateTime.MinValue;
        /// <summary>
        /// The last time the file was accessed.
        /// </summary>
        public DateTime AccessedUtc {
            get { return accessedUtc; }
        }
        private volatile DateTime updatedUtc = DateTime.MinValue;
        /// <summary>
        /// The Created date of the cached file - the last time the cached file was written to
        /// </summary>
        public DateTime UpdatedUtc {
            get { return updatedUtc; }
        }
    }
}
