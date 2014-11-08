/* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ImageResizer.Plugins.DiskCache.Cleanup;
using System.IO;
using ImageResizer.Configuration.Issues;
using System.Diagnostics;
using ImageResizer.Configuration.Logging;
using System.Globalization;

namespace ImageResizer.Plugins.DiskCache {
    public class CleanupWorker : IssueSink, IDisposable {

        Thread t = null;
        EventWaitHandle _queueWait = new AutoResetEvent(false);
        EventWaitHandle _quitWait = new AutoResetEvent(false);
        CleanupStrategy cs = null;
        CleanupQueue queue = null;
        ICleanableCache cache = null;
        ILoggerProvider lp = null;
        /// <summary>
        /// Creates and starts a thread that consumes the queue, pausing until notified when 'queue' empties.
        /// </summary>
        /// <param name="lp"></param>
        /// <param name="cs"></param>
        /// <param name="queue"></param>
        /// <param name="cache"></param>
        public CleanupWorker(ILoggerProvider lp, CleanupStrategy cs, CleanupQueue queue, ICleanableCache cache):base("DiskCache-CleanupWorker") {
            this.cs = cs;
            this.queue = queue;
            this.cache = cache;
            this.lp = lp;
            t = new Thread(main);
            t.IsBackground = true;
            t.Start();
        }
        /// <summary>
        /// Tells the worker to check the queue for more work.
        /// </summary>
        public void MayHaveWork() {
            _queueWait.Set();
        }

        /// <summary>
        /// The last time a cache folder exceeded both the optimum and maximum limits for file count. 
        /// If recent, indicates a misconfiguration; the subfolders="" count needs to be increased.
        /// </summary>
        protected long lastFoundItemsOverMax = DateTime.MinValue.Ticks;

        /// <summary>
        /// The last time a cache query came through
        /// </summary>
        protected long lastBusy = DateTime.MinValue.Ticks;
        /// <summary>
        /// Tells the worker to avoid work for a little bit.
        /// </summary>
        public void BeLazy() {
            lock(_timesLock) lastBusy = DateTime.UtcNow.Ticks;
            
        }
        /// <summary>
        /// The last time we did work (or attempted to do work, failing if the queue was empty)
        /// </summary>
        protected long lastWorked = DateTime.MinValue.Ticks;

        /// <summary>
        /// When true, indicates that another process is managing cleanup operations - this thread is idle, waiting for the other process to end before it can pick up work.
        /// </summary>
        public bool ExteralProcessCleaning { get { return this.otherProcessManagingCleanup; } }
        /// <summary>
        /// When true, indicates that another process is managing cleanup operations - this thread is idle, waiting for the other process to end before it can pick up work.
        /// </summary>
        protected bool otherProcessManagingCleanup = false;
        protected readonly object _timesLock = new object();
        /// <summary>
        /// Thread runs this method.
        /// </summary>
        protected void main() {
            try {
                mainInner();
            } catch (Exception ex) {
                if (Debugger.IsAttached) throw;
                if (lp.Logger != null) lp.Logger.Error("Contact support! A critical (and unexpected) exception occured in the disk cache cleanup worker thread. This needs to be investigated. {0}", ex.Message + ex.StackTrace);
                this.AcceptIssue(new Issue("Contact support! A critical (and unexpected) exception occured in the disk cache cleanup worker thread. This needs to be investigated. ", ex.Message + ex.StackTrace, IssueSeverity.Critical));
            }
        }
        /// <summary>
        /// Processes work items from the queue, using at most 50%
        /// </summary>
        protected void mainInner(){
            //TODO: Verify that GetHashCode() is the same between .net 2 and 4. 
            string mutexKey = "ir.cachedir:" + cache.PhysicalCachePath.ToLowerInvariant().GetHashCode().ToString("x", NumberFormatInfo.InvariantInfo);

            //Sleep for the duration requested before trying anything. 
            _quitWait.WaitOne(cs.StartupDelay);

            Mutex cleanupLock = null;
            bool hasLock = false;
           
            try {
                //Try to create and lock the mutex, or else open and lock it.
                try{
                    cleanupLock = new Mutex(true, mutexKey, out hasLock);
                }catch(UnauthorizedAccessException){
                    hasLock = false;
                }
         
                //Start the work loop
                while (true) {
                    //Check for shutdown
                    if (shuttingDown) return;

                    //Try to acquire a reference to the lock if we didn't have access last time.
                    if (cleanupLock == null) {
                        //In this case, another process (running as another user account) has opened the lock. Eventually it may be garbage collected.
                        try {
                            cleanupLock = new Mutex(true, mutexKey, out hasLock);
                        } catch (UnauthorizedAccessException) {
                            hasLock = false;
                        }
                    }

                    otherProcessManagingCleanup = !hasLock;
                    if (!hasLock) {
                        //If we have a reference, Wait until the other process calls ReleaseMutex(), or for 30 seconds, whichever is shorter.
                        if (cleanupLock != null) hasLock = cleanupLock.WaitOne(30000);
                        else Thread.Sleep(10000); //Otherwise just sleep 10s and check again, waiting for the mutex to be garbage collected so we can recreate it.
                    }
                    otherProcessManagingCleanup = !hasLock;
                    if (!hasLock) {
                        //Still no luck, someone else is managing things...
                        //Clear out the todo-list
                        queue.Clear();
                        //Get back to doing nothing.
                        continue;
                    }
                    

                    //Is it time to do some work?
                    bool noWorkInTooLong = false; //Has it been too long since we did something?
                    lock (_timesLock) noWorkInTooLong = (DateTime.UtcNow.Subtract(new DateTime(lastWorked)) > cs.MaxDelay);

                    bool notBusy = false; //Is the server busy recently?
                    lock (_timesLock) notBusy = (DateTime.UtcNow.Subtract(new DateTime(lastBusy)) > cs.MinDelay);
                    //doSomeWork keeps being true in absence of incoming requests

                    //If the server isn't busy, or if this worker has been lazy to long long, do some work and time it.
                    Stopwatch workedForTime = null;
                    if (noWorkInTooLong || notBusy) {
                        workedForTime = new Stopwatch();
                        workedForTime.Start();
                        DoWorkFor(cs.OptimalWorkSegmentLength);
                        lock (_timesLock) lastWorked = DateTime.UtcNow.Ticks; 
                        workedForTime.Stop();
                    }

                    //Check for shutdown
                    if (shuttingDown) return;

                    
                    //Nothing to do, queue is empty.
                    if (queue.IsEmpty)
                        //Wait perpetually until notified of more queue items.
                        _queueWait.WaitOne();
                    else if (notBusy)
                        //Don't flood the system even when it's not busy. 50% usage here. Wait for the length of time worked or the optimal work time, whichever is longer.
                        //A directory listing can take 30 seconds sometimes and kill the cpu.
                        _quitWait.WaitOne((int)Math.Max(cs.OptimalWorkSegmentLength.TotalMilliseconds, workedForTime.ElapsedMilliseconds));

                    else {
                        //Estimate how long before we can run more code.
                        long busyTicks = 0;
                        lock (_timesLock) busyTicks = (cs.MinDelay - DateTime.UtcNow.Subtract(new DateTime(lastBusy))).Ticks;
                        long maxTicks = 0;
                        lock (_timesLock) maxTicks = (cs.MaxDelay - DateTime.UtcNow.Subtract(new DateTime(lastWorked))).Ticks;
                        //Use the longer value and add a second to avoid rounding and timing errors.
                        _quitWait.WaitOne(new TimeSpan(Math.Max(busyTicks, maxTicks)) + new TimeSpan(0, 0, 1));
                    } 

                    //Check for shutdown
                    if (shuttingDown) return;
                }
            } finally {
                if (hasLock) cleanupLock.ReleaseMutex();
                otherProcessManagingCleanup = false;
            }
        }

        public override IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>(base.GetIssues());
            if (ExteralProcessCleaning) 
                issues.Add(new Issue("An external process indicates it is managing cleanup of the disk cache. " + 
                "This process is not currently managing disk cache cleanup. If configured as a web garden, keep in mind that the negligible performance gains are likely to be outweighed by the loss of cache optimization quality.", IssueSeverity.Warning));
            lock (_timesLock) {
                if (this.lastFoundItemsOverMax > (DateTime.UtcNow.Subtract(new TimeSpan(0, 5, 0)).Ticks))
                    issues.Add(new Issue("Your cache configuration may not be optimal. If this message persists, you should increase the 'subfolders' value in the <diskcache /> element in Web.config",
                        "In the last 5 minutes, a cache folder exceeded both the optimum and maximum limits for file count. This usually indicates that your 'subfolders' setting is too low, and that cached images are being deleted shortly after their creation. \n" +
                        "To estimate the appropriate subfolders value, multiply the total number of images on the site times the average number of size variations, and divide by 400. I.e, 6,400 images with 2 variants would be 32. If in doubt, set the value higher, but ensure there is disk space available.", IssueSeverity.Error));

            }
            return issues;
        }
        /// <summary>
        /// Processes items from the queue for roughly the specified amount of time.
        /// Returns false if the queue was empty.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        protected bool DoWorkFor(TimeSpan length) {
            if (queue.IsEmpty) return false;

            DateTime startedAt = DateTime.UtcNow;
            //Proccess as many items from the queue as possible
            while (DateTime.UtcNow.Subtract(startedAt) < length && !queue.IsEmpty) {
                //Check for shutdown
                if (shuttingDown) return true;
                try {
                    DoTask(queue.Pop());
                } catch (Exception e) {
                    if (Debugger.IsAttached) throw;
                    if (lp.Logger != null) lp.Logger.Error("Failed executing task {0}", e.Message + e.StackTrace);
                    this.AcceptIssue(new Issue("Failed executing task", e.Message + e.StackTrace, IssueSeverity.Critical));
                }
            }
            return true;
        }
        protected volatile bool shuttingDown = false;
        public void Dispose() {
            shuttingDown = true;
            _queueWait.Set();
            _quitWait.Set();
            t.Join(); //Wait for work to stop.
            _queueWait.Close();
            _quitWait.Close();
        }



        protected void DoTask(CleanupWorkItem item) {


            Stopwatch sw = null;
            if (lp.Logger != null) { sw = new Stopwatch(); sw.Start(); }

            if (item.Task == CleanupWorkItem.Kind.RemoveFile)
                RemoveFile(item);
            else if (item.Task == CleanupWorkItem.Kind.CleanFolderRecursive || item.Task == CleanupWorkItem.Kind.CleanFolder) 
                CleanFolder(item, item.Task == CleanupWorkItem.Kind.PopulateFolderRecursive);
            else if (item.Task == CleanupWorkItem.Kind.PopulateFolderRecursive || item.Task == CleanupWorkItem.Kind.PopulateFolder)
                PopulateFolder(item, item.Task == CleanupWorkItem.Kind.PopulateFolderRecursive);
            else if (item.Task == CleanupWorkItem.Kind.FlushAccessedDate) 
                FlushAccessedDate(item);

            if (lp.Logger != null) sw.Stop();
            if (lp.Logger != null) lp.Logger.Trace("{2}ms: Executing task {0} {1} ({3} tasks remaining)", item.Task.ToString(), item.RelativePath, sw.ElapsedMilliseconds.ToString(NumberFormatInfo.InvariantInfo).PadLeft(4), queue.Count.ToString(NumberFormatInfo.InvariantInfo));

            
        }

        protected string addSlash(string s, bool physical) {
            if (string.IsNullOrEmpty(s)) return s; //On empty or null, dont' add aslash.
            if (physical) return s.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            else return s.TrimEnd('/') + '/';
        }

        protected void PopulateFolder(CleanupWorkItem item, bool recursive) {
            //Do the local work.
            if (!cache.Index.GetIsValid(item.RelativePath)) {
                if (lp.Logger != null) {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    cache.Index.populate(item.RelativePath, item.PhysicalPath);
                    sw.Stop();
                    lp.Logger.Trace("{0}ms: Querying filesystem about {1}", sw.ElapsedMilliseconds.ToString(NumberFormatInfo.InvariantInfo).PadLeft(4), item.RelativePath);
                } else 
                    cache.Index.populate(item.RelativePath, item.PhysicalPath);
            }

            if (recursive) {
                //Queue the recursive work.
                IList<string> names = cache.Index.getSubfolders(item.RelativePath);
                List<CleanupWorkItem> childWorkItems = new List<CleanupWorkItem>(names.Count);
                foreach (string n in names)
                    childWorkItems.Add(new CleanupWorkItem(CleanupWorkItem.Kind.PopulateFolderRecursive, addSlash(item.RelativePath,false) + n, addSlash(item.PhysicalPath,true) + n));
                queue.InsertRange(childWorkItems);
            }
        }

        protected void RemoveFile(CleanupWorkItem item) {
            //File names are never embedded into the first item, they are provided on-demand by a LazyProvider
            LazyTaskProvider provider = item.LazyProvider;
            item = provider();
            if (item == null) return; //The provider is out of possible items
            item.LazyProvider = provider; //So if this item fails, we can queue 'item' again and the next task run will get the next alternative.

            bool removedFile = false;

            cache.Locks.TryExecute(item.RelativePath, 10, delegate() {

                //If the file is already gone, consider the mission a succes.
                if (!System.IO.File.Exists(item.PhysicalPath)) {
                    cache.Index.setCachedFileInfo(item.RelativePath, null);
                    removedFile = true;
                    return;
                }
                //Cool, we got a lock on the file.
                //Remove it from the cache. Better a miss than an invalidation.
                cache.Index.setCachedFileInfo(item.RelativePath, null);
                try {
                    System.IO.File.Delete(item.PhysicalPath);
                } catch (IOException) {
                    return; //The file is in use, or has an open handle. - try the next file.
                } catch (UnauthorizedAccessException) {
                    return; //Invalid NTFS permissions or readonly file.  - try the next file
                }

                cache.Index.setCachedFileInfo(item.RelativePath, null); //In case it crossed paths.
                removedFile = true;
            });

            //If we didn't remove a file, insert the task back in the queue for the next iteration.
            if (!removedFile) queue.Insert(item);
        }


        protected void CleanFolder(CleanupWorkItem item, bool recursive) {
            //If we don't have an up-to-date folder level, we can't work..
            if (!cache.Index.GetIsValid(item.RelativePath)) {
                //Put this task back where it was, but with a 'populate/populaterecursive' right before it.
                //We could actually make this Populate non-recursive, since the recursive Clean would just insert Populates beforehand anyway.
                queue.InsertRange(new CleanupWorkItem[]{
                        new CleanupWorkItem(recursive ? CleanupWorkItem.Kind.PopulateFolderRecursive : CleanupWorkItem.Kind.PopulateFolder,item.RelativePath,item.PhysicalPath),
                        item});
                return;
            }

            string baseRelative = addSlash(item.RelativePath, false);
            string basePhysical = addSlash(item.PhysicalPath, true);

            //Ok, it's valid.
            //Queue the recursive work.
            if (item.Task == CleanupWorkItem.Kind.CleanFolderRecursive) {
                IList<string> names = cache.Index.getSubfolders(item.RelativePath);
                List<CleanupWorkItem> childWorkItems = new List<CleanupWorkItem>(names.Count);
                foreach (string n in names)
                    childWorkItems.Add(new CleanupWorkItem(CleanupWorkItem.Kind.CleanFolderRecursive, baseRelative + n, basePhysical + n));
                queue.InsertRange(childWorkItems);
            }

            //Now do the local work
            int files = cache.Index.getFileCount(item.RelativePath);

            //How much are we over?
            int overMax = Math.Max(0, files - cs.MaximumItemsPerFolder);
            int overOptimal = Math.Max(0, (files - overMax) - cs.TargetItemsPerFolder);

            if (overMax + overOptimal < 1) return; //nothing to do

            if (overMax > 0) lock (_timesLock) lastFoundItemsOverMax = DateTime.UtcNow.Ticks;

            //Make a linked list, like a queue of files. 
            LinkedList<KeyValuePair<string, CachedFileInfo>> sortedList = new LinkedList<KeyValuePair<string, CachedFileInfo>>(
                    cache.Index.getSortedSubfiles(item.RelativePath));

            //This callback will execute (overMax) number of times
            CleanupWorkItem obsessive = new CleanupWorkItem(CleanupWorkItem.Kind.RemoveFile, delegate() {
                //Pop the next item
                KeyValuePair<string, CachedFileInfo> file;
                while (sortedList.Count > 0) {
                    file = sortedList.First.Value; sortedList.RemoveFirst();
                    if (cs.ShouldRemove(baseRelative + file.Key, file.Value, true)) {
                        return new CleanupWorkItem(CleanupWorkItem.Kind.RemoveFile, baseRelative + file.Key, basePhysical + file.Key);
                    }
                }
                return null; //No matching items left.
            });

            CleanupWorkItem relaxed = new CleanupWorkItem(CleanupWorkItem.Kind.RemoveFile, delegate() {
                //Pop the next item
                KeyValuePair<string, CachedFileInfo> file;
                while (sortedList.Count > 0) {
                    file = sortedList.First.Value; sortedList.RemoveFirst();
                    if (cs.ShouldRemove(baseRelative + file.Key, file.Value, false)) {
                        return new CleanupWorkItem(CleanupWorkItem.Kind.RemoveFile, baseRelative + file.Key, basePhysical + file.Key);
                    }
                }
                return null; //No matching items left.
            });
            //The 'obsessive' ones must be processed first, thus added last.
            for (int i = 0; i < overOptimal; i++) queue.Insert(relaxed);
            for (int i = 0; i < overMax; i++) queue.Insert(obsessive);


        }

        public void FlushAccessedDate(CleanupWorkItem item) {
            CachedFileInfo c = cache.Index.getCachedFileInfo(item.RelativePath);
            if (c == null) return; //File was already deleted, nothing to do.
            try{
                File.SetLastAccessTimeUtc(item.PhysicalPath, c.AccessedUtc);
                //In both of these exception cases, we don't care.
            }catch (FileNotFoundException){
            }catch (UnauthorizedAccessException){
            }
        }

    }
}
