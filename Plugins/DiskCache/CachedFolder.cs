using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.DiskCache {
    /// <summary>
    /// Represents a cached view of a folder of cached items
    /// </summary>
    public class CachedFolder {
        private volatile bool isPopulated = false;

        public bool IsPopulated {
            get { return isPopulated; }
            set { isPopulated = value; }
        }

        protected Dictionary<string, CachedFolder> folders = new Dictionary<string, CachedFolder>(StringComparer.OrdinalIgnoreCase);

        protected Dictionary<string, CachedFileInfo> files = new Dictionary<string, CachedFileInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns null if (a) the file doesn't exist, or (b) the file isn't populated. Calling code should always fall back to filesystem calls on a null result.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public CachedFileInfo getCachedFileInfo(string relativePath) {
            int slash = relativePath.IndexOf('/');
            if (slash < 0) {
                CachedFileInfo f;
                if (files.TryGetValue(relativePath, out f)) return f; //cache hit
            } else {
                //Try to access subfolder
                string folder = relativePath.Substring(0, slash);
                CachedFolder f;
                if (!folders.TryGetValue(folder, out f)) f = null;
                //Recurse if possible
                if (f != null) return f.getCachedFileInfo(relativePath.Substring(slash + 1));
            }
            return null; //cache miss or file not found
        }

        public CachedFileInfo getFileInfo(string relativePath, string physicalPath) {
            CachedFileInfo f = getCachedFileInfo(relativePath);

        }

    }

}
