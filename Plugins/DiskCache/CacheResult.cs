/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.DiskCache {
    public enum CacheQueryResult {
        /// <summary>
        /// Failed to acquire a lock on the cached item within the timeout period
        /// </summary>
        Failed,
        /// <summary>
        /// The item wasn't cached, but was successfully added to the cache
        /// </summary>
        Miss,
        /// <summary>
        /// The item was already in the cache.
        /// </summary>
        Hit
    }
    public class CacheResult {
        public CacheResult(CacheQueryResult result, string physicalPath, string relativePath) {
            this.result = result;
            this.physicalPath = physicalPath;
            this.relativePath = relativePath;
        }

        private string physicalPath;
        /// <summary>
        /// The physical path to the cached item
        /// </summary>
        public string PhysicalPath {
            get { return physicalPath; }
        }

        private string relativePath;
        /// <summary>
        /// The path relative to the cache
        /// </summary>
        public string RelativePath {
            get { return relativePath; }
        }

        private CacheQueryResult result;
        /// <summary>
        /// The result of the cache check
        /// </summary>
        public CacheQueryResult Result {
            get { return result; }
            set { result = value; }
        }
    }
}
