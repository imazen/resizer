/* Copyright (c) 2014 Imazen See license.txt for your rights. */
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

    
        /// <summary>
        /// Should be a delegate a CleanupWorkItem instance (which doesn't have LazyProvider value, but has RelativePath and PhyscialPath values). May return null
        /// </summary>
        public LazyTaskProvider LazyProvider {get;  set;}
        
        /// <summary>
        /// Cache-relative path 
        /// </summary>
        public string RelativePath { get; private set;}
        /// <summary>
        /// Physcial path
        /// </summary>
        public string PhysicalPath {get; private set;}
        public CleanupWorkItem(Kind task, string relativePath, string physicalPath) {
            this.Task = task;
            this.RelativePath = relativePath;
            this.PhysicalPath = physicalPath;


            if (this.RelativePath.StartsWith("/")) {
                Debug.WriteLine("Invalid relativePath value - should never have leading slash!");
            }
        }

        public CleanupWorkItem(Kind task, LazyTaskProvider callback) {
            this.Task = task;
            this.LazyProvider = callback;
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


        public Kind Task{ get; set; }

    }
}
