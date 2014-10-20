/* Copyright (c) 2014 Imazen See license.txt for your rights. */
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
        /// <summary>
        /// Uses old.AccessedUtc if it is newer than FileInfo.LastAccessTimeUtc
        /// </summary>
        /// <param name="f"></param>
        /// <param name="old"></param>
        public CachedFileInfo(FileInfo f, CachedFileInfo old) {
            modifiedUtc = f.LastWriteTimeUtc;
            accessedUtc = f.LastAccessTimeUtc;
            if (old != null && accessedUtc < old.accessedUtc) accessedUtc = old.accessedUtc; //Use the larger value
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
        private  DateTime modifiedUtc = DateTime.MinValue;
        /// <summary>
        /// The modified date of the source file that the cached file is based on.
        /// </summary>
        public DateTime ModifiedUtc {
            get { return modifiedUtc; }
        }
        private  DateTime accessedUtc = DateTime.MinValue;
        /// <summary>
        /// The last time the file was accessed. Will not match NTFS date, this value is updated by DiskCache.
        /// When first loaded from NTFS, it will be granular to about an hour, due to NTFS delayed write. Also, windows Vista and higher never write accessed dates. 
        /// We update this value in memory, and flush it to disk lazily. 
        /// </summary>
        public DateTime AccessedUtc {
            get { return accessedUtc; }
        }
        private  DateTime updatedUtc = DateTime.MinValue;
        /// <summary>
        /// The Created date of the cached file - the last time the cached file was written to
        /// </summary>
        public DateTime UpdatedUtc {
            get { return updatedUtc; }
        }
    }
}
