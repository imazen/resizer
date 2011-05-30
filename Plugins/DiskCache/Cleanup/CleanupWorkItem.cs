/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace ImageResizer.Plugins.DiskCache {

    public delegate CleanupWorkItem LazyTaskProvider();
    
    /// <summary>
    /// An item in the work queue
    /// </summary>
    public class CleanupWorkItem {

        private string relativePath = null;

        private LazyTaskProvider lazyProvider = null;
        /// <summary>
        /// Should be a delegate a CleanupWorkItem instance (which doesn't have LazyProvider value, but has RelativePath and PhyscialPath values). May return null
        /// </summary>
        public LazyTaskProvider LazyProvider
        {
            get { return lazyProvider; }
            set { lazyProvider = value; }
        }
        /// <summary>
        /// Cache-relative path 
        /// </summary>
        public string RelativePath {
            get { return relativePath; }
        }
        private string physicalPath = null;
        /// <summary>
        /// Physcial path
        /// </summary>
        public string PhysicalPath {
            get { return physicalPath; }
        }
        public CleanupWorkItem(Kind task, string relativePath, string physicalPath) {
            this.task = task;
            this.relativePath = relativePath;
            this.physicalPath = physicalPath;
            if (this.relativePath.StartsWith("/")) {
                Debug.WriteLine("Invalid relativePath value - should never have leading slash!");
            }
        }

        public CleanupWorkItem(Kind task, LazyTaskProvider callback) {
            this.task = task;
            this.lazyProvider = callback;
        }
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override bool Equals(object obj) {
            CleanupWorkItem other = obj as CleanupWorkItem;
            if (other == null) return false;

            return (other.Task == Task && other.RelativePath == RelativePath && other.PhysicalPath == PhysicalPath && other.LazyProvider == LazyProvider);
        }

        public enum Kind {
            /// <summary>
            /// If the .IsValid is false, populates the folder, enqueing more PopulateFolderRecursive items for all subfolders discovered. Sets IsValid to true  
            /// </summary>
            PopulateFolderRecursive,
            /// <summary>
            /// Requires a valid folder.  
            /// </summary>
            CleanFolderRecursive,
            /// <summary>
            /// Cleans the folder, enqueing RemoveFile tasks for everything that needs to be removed.
            /// </summary>
            CleanFolder,
            /// <summary>
            /// Populates (non-recursive) the files and folders inside the specified directory.
            /// </summary>
            PopulateFolder,
            /// <summary>
            /// Removes a single file, with the file and dir being determined at executing time via the LazyProvider delegate.
            /// </summary>
            RemoveFile,
            /// <summary>
            /// Calls File.SetLastAccessedTimeUtc() using the in-memory value, if present.
            /// </summary>
            FlushAccessedDate

        }

        private Kind task = Kind.CleanFolderRecursive;


        public Kind Task {
            get { return task; }
            set { task = value; }
        }

    }
}
