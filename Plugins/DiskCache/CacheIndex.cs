using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ImageResizer.Plugins.DiskCache {
    public class CacheIndex {
        /// <summary>
        /// Returns true if the specified file exists on the filesystem.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="physicalPath"></param>
        /// <param name="doubleCheck">True to *always* check with System.IO, even if the cache says that the file exists</param>
        /// <returns></returns>
        public bool Exists(string relativePath, string physicalPath, bool doubleCheck) {

        }
        /// <summary>
        /// Returns true if the specified file has the specified modified date.
        /// </summary>
        /// <param name="modDate"></param>
        /// <param name="relativePath"></param>
        /// <param name="physicalPath"></param>
        /// <param name="doubleCheckExists">True to *always* verify the file exists with System.IO, even if the cache says the file exists</param>
        /// <returns></returns>
        public bool ModifiedMatches(DateTime modDate, string relativePath, string physicalPath, bool doubleCheckExists) {
        }
        /// <summary>
        /// Updates the dates on the specified file
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="modDate"></param>
        /// <param name="createdDate"></param>
        /// <returns></returns>
        public bool UpdateTimes(string relativePath, DateTime modDate, DateTime createdDate) {
        }
        /// <summary>
        /// Updates the 'accessed' date on the specified file
        /// </summary>
        /// <param name="relativePath"></param>
        public void UsedFile(string relativePath) {

        }

        public void AddFile(string relativePath, CachedFileInfo info) {
        }
        public bool RemoveFile(string relativePath) {
        }
        public CachedFolder GetCopy(string relativePath) {
        }


        /// <summary>
        /// Returns true if the specified file exists. 
        /// </summary>
        /// <returns></returns>
        public bool Exists(string localCachedFile) {
            if (!System.IO.File.Exists(localCachedFile)) return false;
            return true;
        }
        /// <summary>
        ///  Returns true if localCachedFile exists and matches sourceDataModifiedUTC.
        /// </summary>
        /// <param name="sourceDataModifiedUTC"></param>
        /// <param name="localCachedFile"></param>
        /// <returns></returns>
        public bool IsCachedVersionValid(DateTime sourceDataModifiedUTC, string localCachedFile) {
            if (string.IsNullOrEmpty(localCachedFile)) return false;
            if (!Exists(localCachedFile)) return false;


            //When we save cached files to disk, we set the write time to that of the source file.
            //This allows us to track if the source file has changed.

            DateTime cached = System.IO.File.GetLastWriteTimeUtc(localCachedFile);
            return RoughCompare(cached, sourceDataModifiedUTC);
        }
        /// <summary>
        /// Returns true if both dates are equal (to the nearest 200th of a second)
        /// </summary>
        /// <param name="modifiedOn"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        protected static bool RoughCompare(DateTime d1, DateTime d2) {
            return Math.Abs(d1.Ticks - d2.Ticks) < TimeSpan.TicksPerMillisecond * 5;
        }
    }
}
