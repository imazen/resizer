using ImageResizer.Configuration.Issues;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Plugins
{
    enum FolderOptions
    {
        Default, 
        CreateIfMissing
    }

    // TODO: use a friendlier ACL when creating directories and files?

    // Does make files written world-readable (but not writable)
    class MultiFolderStorage
    {
        IIssueReceiver sink;
        string issueSource;
        string[] candidateFolders;
        FolderOptions options;
        string dataKind = "file";

        public MultiFolderStorage(string issueSource, string dataKind, IIssueReceiver sink, string[] candidateFolders, FolderOptions options)
        {
            this.dataKind = dataKind;
            this.issueSource = issueSource;
            this.candidateFolders = candidateFolders;
            this.sink = sink;
            this.options = options;
        }

        ConcurrentBag<string> badReadLocations = new ConcurrentBag<string>();
        void AddBadReadLocation(string path, IIssue i)
        {
            badReadLocations.Add(path);
            if (i != null) sink.AcceptIssue(i);
        }

        ConcurrentBag<string> badWriteLocations = new ConcurrentBag<string>();
        void AddBadWriteLocation(string path, IIssue i)
        {
            badWriteLocations.Add(path);
            if (i != null) sink.AcceptIssue(i);
        }

        ReaderWriterLockSlim filesystem = new ReaderWriterLockSlim();

        /// <summary>
        /// Returns false if any copy of the file failed to delete. Doesn't reference failed directories
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool TryDelete(string filename)
        {
            bool failedAtSomething = false;
            try
            {
                filesystem.EnterWriteLock();
                foreach (var dest in candidateFolders)
                {
                    var path = Path.Combine(dest, filename);
                    try
                    {
                        if (File.Exists(path)) File.Delete(path);
                    }
                    catch (Exception e)
                    {
                        sink.AcceptIssue(new Issue(this.issueSource, "Failed to delete " + dataKind + " at location " + path, e.ToString(), IssueSeverity.Warning));
                        failedAtSomething = true;
                    }
                }
                return !failedAtSomething;
            }
            finally
            {
                filesystem.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns true if 1 or more copies of the value were written
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryDiskWrite(string filename, string value)
        {
            if (value == null)
            {
                return TryDelete(filename);
            }
            else
            {
                try
                {
                    filesystem.EnterWriteLock();
                    int successfulWrites = 0;
                    foreach (var dest in candidateFolders.Except(badWriteLocations))
                    {
                        if (options.HasFlag(FolderOptions.CreateIfMissing))
                        {
                            try { 
                                if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);
                            }
                            catch (Exception e)
                            {
                                AddBadWriteLocation(dest, new Issue(this.issueSource, "Failed to create directory " + dest, e.ToString(), IssueSeverity.Warning));
                            }
                        }else if (!Directory.Exists(dest))
                        {
                            AddBadWriteLocation(dest, null);
                        }
                        if (Directory.Exists(dest))
                        {
                            var path = Path.Combine(dest, filename);
                            try
                            {

                                File.WriteAllText(path, value, UTF8Encoding.UTF8);

                                // Make world readable
                                var sec = File.GetAccessControl(path);
                                SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                                sec.AddAccessRule(
                                    new FileSystemAccessRule(everyone,
                                    FileSystemRights.Read, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
                                File.SetAccessControl(path, sec);

                                successfulWrites++;
                            }
                            catch (Exception e)
                            {
                                AddBadWriteLocation(dest, new Issue(this.issueSource, "Failed to write " + dataKind + " to location " + path, e.ToString(), IssueSeverity.Warning));
                            }
                        }
                    }
                    if (successfulWrites > 0) return true;
                }
                finally
                {
                    filesystem.ExitWriteLock();
                }
                sink.AcceptIssue(new Issue(this.issueSource,"Unable to cache " + dataKind + " to disk in any location.", null, IssueSeverity.Error));
                return false;
            }
        }

        /// <summary>
        /// Returns null if the file is missing or the read failed.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public string TryDiskRead(string filename)
        {
            bool readFailed = false; //To tell non-existent files apart from I/O errors
            try
            {
                filesystem.EnterReadLock();
                foreach (var dest in candidateFolders.Except(badReadLocations))
                {
                    var path = Path.Combine(dest, filename);
                    if (!badReadLocations.Contains(path) && File.Exists(path))
                    {
                        try
                        {
                            return File.ReadAllText(path, UTF8Encoding.UTF8);
                        }
                        catch (Exception e)
                        {
                            readFailed = true;
                            AddBadReadLocation(path, new Issue(this.issueSource,  "Failed to read " + dataKind + " from location " + path, e.ToString(), IssueSeverity.Warning));
                        }
                    }
                }
            }
            finally
            {
                filesystem.ExitReadLock();
            }
            if (readFailed)
            {
                sink.AcceptIssue(new Issue(this.issueSource, "Unable to read " + dataKind + " from disk despite its existence.", null, IssueSeverity.Error));
            }
            return null;
        }
        /// <summary>
        /// Returns null if the file is missing or the read failed.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public DateTime? TryGetLastWriteTimeUtc(string filename)
        {
            bool readFailed = false; //To tell non-existent files apart from I/O errors
            try
            {
                filesystem.EnterReadLock();
                foreach (var dest in candidateFolders.Except(badReadLocations))
                {
                    var path = Path.Combine(dest, filename);
                    if (!badReadLocations.Contains(path) && File.Exists(path))
                    {
                        try
                        {
                            return File.GetLastWriteTimeUtc(path);
                        }
                        catch (Exception e)
                        {
                            readFailed = true;
                            AddBadReadLocation(path, new Issue(this.issueSource, "Failed to read write time of " + dataKind + " from location " + path, e.ToString(), IssueSeverity.Warning));
                        }
                    }
                }
            }
            finally
            {
                filesystem.ExitReadLock();
            }
            if (readFailed)
            {
                sink.AcceptIssue(new Issue(this.issueSource, "Unable to read write time of " + dataKind + " from disk despite its existence.", null, IssueSeverity.Error));
            }
            return null;
        }
    }
}
