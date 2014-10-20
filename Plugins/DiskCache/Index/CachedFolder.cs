/* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ImageResizer.Plugins.DiskCache {
    public delegate void FileDisappearedHandler(string relativePath, string physicalPath);

    /// <summary>
    /// Represents a cached view of a folder of cached items
    /// </summary>
    public class CachedFolder {
        public CachedFolder() { }

        protected readonly object _sync = new object();

        private volatile bool isValid = false;
        /// <summary>
        /// Defaults to false. Set to true immediately after being refreshed from the filesystem.
        /// Set to false if a file disappears from the filesystem cache without the cache index being notified first.
        /// Used by the cleanup system - not of importance to the cache write system.
        /// </summary>
        public bool IsValid {
            get { return isValid; }
            set { isValid = value; }
        }




        /// <summary>
        /// Fired when a file disappears from the cache folder without the cache index knowing about it.
        /// </summary>
        public event FileDisappearedHandler FileDisappeared;

        protected StringComparer KeyComparer { get { return StringComparer.OrdinalIgnoreCase; } }
 

        protected Dictionary<string, CachedFolder> folders = new Dictionary<string, CachedFolder>(StringComparer.OrdinalIgnoreCase);

        protected Dictionary<string, CachedFileInfo> files = new Dictionary<string, CachedFileInfo>(StringComparer.OrdinalIgnoreCase);


        public virtual void clear() {
            lock (_sync) {
                IsValid = false;
                folders.Clear();
                files.Clear();
            }
        }

        /// <summary>
        /// Returns null if (a) the file doesn't exist, or (b) the file isn't populated. Calling code should always fall back to filesystem calls on a null result.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public virtual CachedFileInfo getCachedFileInfo(string relativePath) {
            relativePath = checkRelativePath(relativePath);
            lock (_sync) {
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
        }

        /// <summary>
        /// Sets the CachedFileInfo object for the specified path, creating any needed folders along the way.
        /// If 'null', the item will be removed, and no missing folder will be created.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="info"></param>
        public virtual void setCachedFileInfo(string relativePath, CachedFileInfo info) {
            relativePath = checkRelativePath(relativePath);
            lock (_sync) {
                int slash = relativePath.IndexOf('/');
                if (slash < 0) {
                    //Set or remove the file
                    if (info == null)
                        files.Remove(relativePath);
                    else
                        files[relativePath] = info;
                } else {
                    //Try to access subfolder
                    string folder = relativePath.Substring(0, slash);
                    CachedFolder f;
                    if (!folders.TryGetValue(folder, out f)) f = null;


                    if (info == null && f == null) return; //If the folder doesn't exist, the file definitely doesn't. Already accomplished.
                    //Create it if it doesn't exist
                    if (f == null) f = folders[folder] = new CachedFolder();
                    //Recurse if possible
                    f.setCachedFileInfo(relativePath.Substring(slash + 1), info);
                }
            }
        }
        /// <summary>
        /// Tries to set the AccessedUtc of the specified file to the current date (just in memory, not on the filesystem).
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public bool bumpDateIfExists(string relativePath) {
            relativePath = checkRelativePath(relativePath);
            lock (_sync) {
                int slash = relativePath.IndexOf('/');
                if (slash < 0) {
                    //Update the accessed date.
                    CachedFileInfo old;
                    if (files.TryGetValue(relativePath,out old)) files[relativePath] = new CachedFileInfo(old, DateTime.UtcNow);
                    return true; //We updated it!
                } else {
                    //Try to access subfolder
                    string folder = relativePath.Substring(0, slash);
                    CachedFolder f = null;
                    if (!folders.TryGetValue(folder, out f)) return false;//If the folder doesn't exist, quit
                    if (f == null) return false; //If the folder is null, quit!

                    //Recurse if possible
                    return f.bumpDateIfExists(relativePath.Substring(slash + 1));
                }
            }
        }

        /// <summary>
        /// Gets a CachedFileInfo object for the file even if it isn't in the cache (falls back to the filesystem)
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="physicalPath"></param>
        /// <returns></returns>
        public virtual CachedFileInfo getFileInfo(string relativePath, string physicalPath) {
            relativePath = checkRelativePath(relativePath);
            lock (_sync) {
                CachedFileInfo f = getCachedFileInfo(relativePath);
                //On cache miss or no file
                if (f == null && System.IO.File.Exists(physicalPath)) {
                    //on cache miss
                    f = new CachedFileInfo(new System.IO.FileInfo(physicalPath));
                    //Populate cache
                    setCachedFileInfo(relativePath, f);
                }
                return f;//Null only if the file doesn't exist.
            }
        }
        /// <summary>
        /// Verifies the file exists before returning the cached data. 
        /// Discrepancies in file existence result in OnFileDisappeard being fired.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="physicalPath"></param>
        /// <returns></returns>
        public virtual CachedFileInfo getFileInfoCertainExists(string relativePath, string physicalPath) {
            relativePath = checkRelativePath(relativePath);
            bool fireEvent = false;
            CachedFileInfo f = null;
            lock (_sync) {
                bool exists = System.IO.File.Exists(physicalPath);

                f = getCachedFileInfo(relativePath);
                //cache miss
                if (f == null && exists) {
                    //on cache miss
                    f = new CachedFileInfo(new System.IO.FileInfo(physicalPath));
                    //Populate cache
                    setCachedFileInfo(relativePath, f);
                }
                //cache wrong, discrepancy. File deleted by external actor
                if (f != null && !exists) {
                    f = null;
                    clear(); //Clear the cache completely.
                    fireEvent = true;
                }
            }
            //Fire the event outside of the lock.
            if (fireEvent && FileDisappeared != null) FileDisappeared(relativePath, physicalPath);

            return f;//Null only if the file doesn't exist.
        }

        /// <summary>
        /// Returns the value of IsValid on the specified folder if present, or 'false' if not present.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public bool GetIsValid(string relativePath) {
            lock (_sync) {
                CachedFolder f = getFolder(relativePath);
                if (f != null) return f.IsValid;
                return false;
            }
        }
        /// <summary>
        /// Not thread safe.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        protected CachedFolder getFolder(string relativePath) {
            return getOrCreateFolder(relativePath, false);
        }
        protected CachedFolder getOrCreateFolder(string relativePath, bool createIfMissing) {
            relativePath = checkRelativePath(relativePath);
            if (string.IsNullOrEmpty(relativePath)) return this;

            int slash = relativePath.IndexOf('/');
            string folder = relativePath;
            if (slash > -1) {
                folder = relativePath.Substring(0, slash);
                relativePath = relativePath.Substring(slash + 1);
            } else relativePath = "";

            CachedFolder f;
            if (!folders.TryGetValue(folder, out f)) {
                if (!createIfMissing) return null;
                else f = folders[folder] = new CachedFolder();
            }
            //Recurse if possible
            if (f != null) return f.getFolder(relativePath);
            //Not found
            return null;

        }

        /// <summary>
        /// returns a list 
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public IList<string> getSubfolders(string relativePath) {
            lock (_sync) {
                CachedFolder f = getFolder(relativePath);
                return new List<string>(f.folders.Keys);
            }
        }

        /// <summary>
        /// returns a dictionary of files. 
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public Dictionary<string, CachedFileInfo> getSubfilesCopy(string relativePath) {
            lock (_sync) {
                CachedFolder f = getFolder(relativePath);
                return new Dictionary<string, CachedFileInfo>(f.files, f.files.Comparer);
            }
        }
        /// <summary>
        /// returns a dictionary of files. 
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public ICollection<KeyValuePair<string, CachedFileInfo>> getSortedSubfiles(string relativePath) {
            lock (_sync) {
                CachedFolder f = getFolder(relativePath);
                if (f == null || f.files.Count < 1) return null;
                //Copy pairs to an array.
                KeyValuePair<string, CachedFileInfo>[] items = new KeyValuePair<string, CachedFileInfo>[f.files.Count];
                int i = 0;
                foreach (KeyValuePair<string, CachedFileInfo> pair in f.files) {
                    items[i] = pair;
                    i++;
                }
                //Sort the pairs on accessed date
                Array.Sort<KeyValuePair<string, CachedFileInfo>>(items, delegate(KeyValuePair<string, CachedFileInfo> a, KeyValuePair<string, CachedFileInfo> b) {
                    return DateTime.Compare(a.Value.AccessedUtc, b.Value.AccessedUtc);
                });


                return items;
            }
        }


        public int getFileCount(string relativePath) {
            lock (_sync) {
                CachedFolder f = getFolder(relativePath);
                return f.files.Count;
            }
        }
        /// <summary>
        /// Refreshes file and folder listing for this folder (non-recursive). Sets IsValid=true afterwards.
        /// 
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="physicalPath"></param>
        public void populate(string relativePath, string physicalPath) {
            //NDJ-added May 29,2011
            //Nothing was setting IsValue=true before.
            populateSubfolders(relativePath, physicalPath);
            populateFiles(relativePath, physicalPath);
            getOrCreateFolder(relativePath, true).IsValid = true;
        }
        /// <summary>
        /// Updates  the 'folders' dictionary to match the folders that exist on disk. ONLY UPDATES THE LOCAL FOLDER
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="physicalPath"></param>
        protected void populateSubfolders(string relativePath, string physicalPath) {
            relativePath = checkRelativePath(relativePath);
            string[] dirs = null;
            try {
                 dirs = System.IO.Directory.GetDirectories(physicalPath);
            } catch (DirectoryNotFoundException) {
                dirs = new string[]{}; //Pretend it's empty. We don't care, the next recursive will get rid of it.
            }
            lock (_sync) {
                CachedFolder f = getOrCreateFolder(relativePath, true);
                Dictionary<string, CachedFolder> newFolders = new Dictionary<string, CachedFolder>(dirs.Length, KeyComparer);
                foreach (string s in dirs) {
                    string local = s.Substring(s.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
                    if (local.StartsWith(".")) continue; //Skip folders that start with a period.
                    if (f.folders.ContainsKey(local)) 
                        newFolders[local] = f.folders[local]; //What if the value is null? does containskey work?
                    else 
                        newFolders[local] = new CachedFolder();
                }
                f.folders = newFolders; //Question - why didn't the folders ge tlisted?
            }
        }
        /// <summary>
        /// Updates the 'files' dictionary to match the files that exist on disk. Uses the accessedUtc values from the previous dictionary if they are newer.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="physicalPath"></param>
        protected void populateFiles(string relativePath, string physicalPath) {
            relativePath = checkRelativePath(relativePath);
            string[] physicalFiles = null;
            try {
                physicalFiles = System.IO.Directory.GetFiles(physicalPath);
            } catch (DirectoryNotFoundException) {
                physicalFiles = new string[] { }; //Pretend it's empty. We don't care, the next recursive will get rid of it.
            }
            Dictionary<string, CachedFileInfo> newFiles = new Dictionary<string, CachedFileInfo>(physicalFiles.Length, KeyComparer);

            CachedFolder f = getOrCreateFolder(relativePath, true);
            foreach (string s in physicalFiles) {
                string local = s.Substring(s.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
                
                //Todo, add a callback that handles exclusion of files
                if (local.EndsWith(".config", StringComparison.OrdinalIgnoreCase)) continue;
                if (local.StartsWith(".")) continue; //Skip files that start with a period

                //What did we have on file?
                CachedFileInfo old = null;
                lock (_sync) {
                    if (!f.files.TryGetValue(relativePath, out old)) old = null;
                }
                newFiles[local] = new CachedFileInfo(new FileInfo(s), old);
            }
            lock (_sync) {
                f.files = newFiles;
            }
        }



        public bool existsCertain(string relativePath, string physicalPath) {
            return getFileInfoCertainExists(relativePath, physicalPath) != null;
        }
        public bool exists(string relativePath, string physicalPath) {
            return getFileInfo(relativePath, physicalPath) != null;
        }
        public bool modifiedDateMatches(DateTime utc, string relativePath, string physicalPath) {
            CachedFileInfo f = getFileInfo(relativePath, physicalPath);
            if (f == null) return false;
            return roughCompare(f.ModifiedUtc, utc);
        }
        public bool modifiedDateMatchesCertainExists(DateTime utc, string relativePath, string physicalPath) {
            CachedFileInfo f = getFileInfoCertainExists(relativePath, physicalPath);
            if (f == null) return false;
            return roughCompare(f.ModifiedUtc, utc);
        }

        /// <summary>
        /// Returns true if both dates are equal to the nearest 200th of a second.
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        protected bool roughCompare(DateTime d1, DateTime d2) {
            return Math.Abs(d1.Ticks - d2.Ticks) < TimeSpan.TicksPerMillisecond * 5;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        protected string checkRelativePath(string relativePath) {
            if (relativePath == null) return relativePath;
            if (relativePath.StartsWith("/") || relativePath.EndsWith("/")) {
                Debug.WriteLine("Invalid relativePath value - should never have leading slash!");
            }
            return relativePath;
        }
    }

}
