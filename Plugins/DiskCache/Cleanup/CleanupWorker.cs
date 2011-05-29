/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ImageResizer.Plugins.DiskCache.Cleanup;
using System.IO;
using ImageResizer.Configuration.Issues;
using System.Diagnostics;

namespace ImageResizer.Plugins.DiskCache {
    public class CleanupWorker : IssueSink, IDisposable {

        Thread t = null;
        EventWaitHandle _queueWait = new AutoResetEvent(false);
        EventWaitHandle _quitWait = new AutoResetEvent(false);
        CleanupStrategy cs = null;
        CleanupQueue queue = null;
        CustomDiskCache cache = null;
        public CleanupWorker(CleanupStrategy cs, CleanupQueue queue, CustomDiskCache cache):base("DiskCache-CleanupWorker") {
            this.cs = cs;
            this.queue = queue;
            this.cache = cache;
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


        protected long lastBusy = DateTime.MinValue.Ticks;
        /// <summary>
        /// Tells the worker to avoid work for a little bit.
        /// </summary>
        public void BeLazy() {
            lock(_timesLock) lastBusy = DateTime.UtcNow.Ticks;
            
        }
        protected long lastWorked = DateTime.MinValue.Ticks;

        protected readonly object _timesLock = new object();
        /// <summary>
        /// Thread runs this method.
        /// </summary>
        protected void main() {
            //Sleep for the duration requested
            _quitWait.WaitOne(cs.StartupDelay);
            //Start the work loop
            while(true){
                //Check for shutdown
                if (shuttingDown) return;

                //Is it time to do some work?
                bool noWorkInTooLong = false;
                lock (_timesLock) noWorkInTooLong = (DateTime.UtcNow.Subtract(new DateTime(lastWorked)) > cs.MaxDelay);
                bool notBusy = false;
                lock (_timesLock) notBusy = (DateTime.UtcNow.Subtract(new DateTime(lastBusy)) > cs.MinDelay);
                //doSomeWork keeps being true in absence of incoming requests

                bool didWork = (noWorkInTooLong || notBusy) && DoWorkFor(cs.OptimalWorkSegmentLength);
                
                //Check for shutdown
                if (shuttingDown) return;

                //Nothing to do, queue is empty.
                if (!didWork && queue.IsEmpty) 
                    //Wait perpetually until notified of more queue items.
                    _queueWait.WaitOne();
                else if (didWork && notBusy) 
                    //Don't flood the system even when it's not busy. 50% usage here.
                    _quitWait.WaitOne(cs.OptimalWorkSegmentLength); 
                else if (didWork && !notBusy) {
                    //Estimate how long before we can run more code.
                    long busyTicks = 0;
                    lock (_timesLock) busyTicks = (cs.MinDelay - DateTime.UtcNow.Subtract(new DateTime(lastBusy))).Ticks;
                    long maxTicks = 0;
                    lock (_timesLock) maxTicks = (cs.MaxDelay - DateTime.UtcNow.Subtract(new DateTime(lastWorked))).Ticks;
                    //Use the longer value and add a second to avoid rounding and timing errors.
                    _quitWait.WaitOne(new TimeSpan(Math.Max(busyTicks,maxTicks)) + new TimeSpan(0,0,1) ); 
                }
                //Check for shutdown
                if (shuttingDown) return;
            }
            
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
                //try {
                    DoTask(queue.Pop());
                //} catch (Exception e) {
                //    if (Debugger.IsAttached) throw;
                        
                //    this.AcceptIssue(new Issue("Failed exeuting task", e.Message + e.StackTrace, IssueSeverity.Critical));
                //}
            }

            lock (_timesLock) lastWorked = DateTime.UtcNow.Ticks;
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
            Debug.WriteLine("Executing task " + item.Task.ToString() + " " + item.RelativePath + " (" + queue.Count.ToString() + " task remaining)");

            //When removing a file, item.RelativePath and item.PhysicalPath might never exist.
            if (item.Task == CleanupWorkItem.Kind.RemoveFile) {
                LazyTaskProvider provider = item.LazyProvider;
                bool removedFile = false;
                while (item != null && !removedFile) {
                    if (provider != null) item = provider(); //Keep asking for the next candidate on failure
                    if (item == null) return; //No more files to try!
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
                        } catch (IOException e) { return; } //Faild to get a file lock.

                        cache.Index.setCachedFileInfo(item.RelativePath, null); //In case it crossed paths.
                        removedFile = true;
                    });
                }
                return;
            }

            string baseRelative = item.RelativePath.Length > 0 ? (item.RelativePath + "/") : "";
            string basePhysical = item.PhysicalPath.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            if (item.Task == CleanupWorkItem.Kind.CleanFolderRecursive || item.Task == CleanupWorkItem.Kind.CleanFolder) {
                if (cache.Index.GetIsValid(item.RelativePath)) {
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

                    //Make a linked list, like a queue of files. 
                    LinkedList<KeyValuePair<string, CachedFileInfo>> sortedList = new LinkedList<KeyValuePair<string, CachedFileInfo>>(
                            cache.Index.getSortedSubfiles(item.RelativePath));

                    //This callback will execute (overMax) number of times
                    CleanupWorkItem obsessive = new CleanupWorkItem(CleanupWorkItem.Kind.RemoveFile, delegate() {
                        //Pop the next item
                        KeyValuePair<string, CachedFileInfo> file;
                        while (sortedList.Count > 0) {
                            file = sortedList.First.Value; sortedList.RemoveFirst();
                            if (cs.MeetsOverMaxCriteria(file.Value)) {
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
                            if (cs.MeetsCleanupCriteria(file.Value)) {
                                return new CleanupWorkItem(CleanupWorkItem.Kind.RemoveFile, baseRelative + file.Key, basePhysical + file.Key);
                            }
                        }
                        return null; //No matching items left.
                    });
                    //The 'obsessive' ones must be processed first, thus added last.
                    for (int i = 0; i < overOptimal; i++) queue.Insert(relaxed);
                    for (int i = 0; i < overMax; i++) queue.Insert(obsessive);

                } else {
                    //Put this item back where it was, but with a 'populate/populaterecursive' right before it.
                    CleanupWorkItem.Kind popKind = item.Task == CleanupWorkItem.Kind.CleanFolderRecursive ? CleanupWorkItem.Kind.PopulateFolderRecursive : CleanupWorkItem.Kind.PopulateFolder;
                    queue.InsertRange(new CleanupWorkItem[]{
                        new CleanupWorkItem(popKind,item.RelativePath,item.PhysicalPath),
                        item});
                    return;
                }
            } else if (item.Task == CleanupWorkItem.Kind.PopulateFolderRecursive ||
                   item.Task == CleanupWorkItem.Kind.PopulateFolder) {

                //Do the local work.
                if (!cache.Index.GetIsValid(item.RelativePath)) {
                    Debug.WriteLine("Querying filesystem about " + item.PhysicalPath);
                    cache.Index.populate(item.RelativePath, item.PhysicalPath);
                }

                if (item.Task == CleanupWorkItem.Kind.PopulateFolderRecursive) {
                    //Queue the recursive work.
                    IList<string> names = cache.Index.getSubfolders(item.RelativePath);
                    List<CleanupWorkItem> childWorkItems = new List<CleanupWorkItem>(names.Count);
                    foreach (string n in names)
                        childWorkItems.Add(new CleanupWorkItem(CleanupWorkItem.Kind.PopulateFolderRecursive, baseRelative + n, basePhysical + n));
                    queue.InsertRange(childWorkItems);
                }
            }
        }


    }
}
