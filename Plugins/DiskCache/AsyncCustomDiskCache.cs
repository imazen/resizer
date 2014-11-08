/* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Caching;
using System.IO;
using ImageResizer.Configuration.Logging;
using System.Diagnostics;
using System.Threading;
using ImageResizer.Plugins.DiskCache.Async;
using System.Globalization;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.DiskCache {

    /// <summary>
    /// Handles access to a disk-based file cache. Handles locking and versioning. 
    /// Supports subfolders for scalability.
    /// </summary>
    public class AsyncCustomDiskCache:ICleanableCache {

        public delegate Task AsyncWriteResult(Stream output);

        public string PhysicalCachePath { get; protected set; }
        protected int subfolders;
        protected ILoggerProvider lp;
        public AsyncCustomDiskCache(ILoggerProvider lp, string physicalCachePath, int subfolders):this(lp,physicalCachePath,subfolders, 1024*1024*10) {

        }
        public AsyncCustomDiskCache(ILoggerProvider lp, string physicalCachePath, int subfolders, long asyncMaxQueuedBytes)
        {
            Locks = new AsyncLockProvider();
            QueueLocks = new AsyncLockProvider();
            Index = new CacheIndex();
            CurrentWrites = new AsyncWriteCollection();
            this.lp = lp;
            this.PhysicalCachePath = physicalCachePath;
            this.subfolders = subfolders;
            this.CurrentWrites.MaxQueueBytes = asyncMaxQueuedBytes;
        }
        /// <summary>
        /// Fired immediately before GetCachedFile return the result value. 
        /// </summary>
        public event CacheResultHandler CacheResultReturned; 


        /// <summary>
        /// Provides string-based locking for file write access.
        /// </summary>
        public ILockProvider Locks {get;protected set;}

        /// <summary>
        /// Provides string-based locking for image resizing (not writing, just processing). Prevents duplication of efforts in asynchronous mode, where 'Locks' is not being used.
        /// </summary>
        public ILockProvider QueueLocks { get; protected set; }

        /// <summary>
        /// Contains all the queued and in-progress writes to the cache. 
        /// </summary>
        public AsyncWriteCollection CurrentWrites {get; private set;}

        /// <summary>
        /// Provides an in-memory index of the cache.
        /// </summary>
        public CacheIndex Index { get; private set; }
        
        /// <summary>
        /// If the cached data exists and is up-to-date, returns the path to it. Otherwise, this function tries to cache the data and return the path.
        /// </summary>
        /// <param name="keyBasis">The basis for the cache key.</param>
        /// <param name="extension">The extension to use for the cached file.</param>
        /// <param name="writeCallback">A method that accepts a Stream argument and writes the data to it.</param>
        /// =<param name="timeoutMs"></param>
        /// <returns></returns>
        public Task<CacheResult> GetCachedFile(string keyBasis, string extension, AsyncWriteResult writeCallback, int timeoutMs)
        {
            return GetCachedFile(keyBasis, extension, writeCallback,  timeoutMs, false);
        }


        /// <summary>
        /// May return either a physical file name or a MemoryStream with the data. 
        /// Faster than GetCachedFile, as writes are (usually) asynchronous. If the write queue is full, the write is forced to be synchronous again.
        /// Identical to GetCachedFile() when asynchronous=false
        /// </summary>
        /// <param name="keyBasis"></param>
        /// <param name="extension"></param>
        /// <param name="writeCallback"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public async Task<CacheResult> GetCachedFile(string keyBasis, string extension, AsyncWriteResult writeCallback, int timeoutMs, bool asynchronous)
        {
            Stopwatch sw = null;
            if (lp.Logger != null) { sw = new Stopwatch(); sw.Start(); }

            //Relative to the cache directory. Not relative to the app or domain root
            string relativePath = new UrlHasher().hash(keyBasis, subfolders, "/") + '.' + extension;

            //Physical path
            string physicalPath = PhysicalCachePath.TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar +
                    relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);


            CacheResult result = new CacheResult(CacheQueryResult.Hit, physicalPath, relativePath);

            bool asyncFailed = false;
            

            //2013-apr-25: What happens if the file is still being written to disk - it's present but not complete? To handle that, we use mayBeLocked.

            bool mayBeLocked = Locks.MayBeLocked(relativePath.ToUpperInvariant());

             //On the first check, verify the file exists using System.IO directly (the last 'true' parameter).
            if (!asynchronous) {
                //On the first check, verify the file exists using System.IO directly (the last 'true' parameter)
                //May throw an IOException if the file cannot be opened, and is locked by an external processes for longer than timeoutMs. 
                //This method may take longer than timeoutMs under absolute worst conditions. 
                if (!await TryWriteFile(result, physicalPath, relativePath, writeCallback, timeoutMs, !mayBeLocked)) {
                    //On failure
                    result.Result = CacheQueryResult.Failed;
                }
            }
            else if (!Index.existsCertain(relativePath, physicalPath) || mayBeLocked)
            {
                
                //Looks like a miss. Let's enter a lock for the creation of the file. This is a different locking system than for writing to the file - far less contention, as it doesn't include the 
                //This prevents two identical requests from duplicating efforts. Different requests don't lock.

                //Lock execution using relativePath as the sync basis. Ignore casing differences. This prevents duplicate entries in the write queue and wasted CPU/RAM usage.
                if (!await ((AsyncLockProvider)QueueLocks).TryExecuteAsync(relativePath.ToUpperInvariant(), timeoutMs,
                    async delegate() {

                        //Now, if the item we seek is in the queue, we have a memcached hit. If not, we should check the index. It's possible the item has been written to disk already.
                        //If both are a miss, we should see if there is enough room in the write queue. If not, switch to in-thread writing. 

                        AsyncWrite t = CurrentWrites.Get(relativePath);

                        if (t != null) result.Data = t.GetReadonlyStream();

                        

                        //On the second check, use cached data for speed. The cached data should be updated if another thread updated a file (but not if another process did).
                        //When t == null, and we're inside QueueLocks, all work on the file must be finished, so we have no need to consult mayBeLocked.
                        if (t == null && !Index.exists(relativePath, physicalPath))
                        {

                            result.Result = CacheQueryResult.Miss;
                            //Still a miss, we even rechecked the filesystem. Write to memory.
                            MemoryStream ms = new MemoryStream(4096);  //4K initial capacity is minimal, but this array will get copied around alot, better to underestimate.
                            //Read, resize, process, and encode the image. Lots of exceptions thrown here.
                            await writeCallback(ms);
                            ms.Position = 0;

                            AsyncWrite w = new AsyncWrite(CurrentWrites,ms, physicalPath, relativePath);
                            if (CurrentWrites.Queue(w, delegate(AsyncWrite job) {
                                try {
                                    Stopwatch swio = new Stopwatch();
                                    
                                    swio.Start();
                                    //We want this to run synchronously, since it's in a background thread already.
                                    if (!TryWriteFile(null, job.PhysicalPath, job.Key, delegate(Stream s) { ((MemoryStream)job.GetReadonlyStream()).CopyToAsync(s); return Task.FromResult(true); }, timeoutMs, true).Result)
                                    {
                                        swio.Stop();
                                        //We failed to lock the file.
                                        if (lp.Logger != null) 
                                            lp.Logger.Warn("Failed to flush async write, timeout exceeded after {1}ms - {0}",  result.RelativePath, swio.ElapsedMilliseconds);
                                        
                                    } else {
                                        swio.Stop();
                                        if (lp.Logger != null)
                                            lp.Logger.Trace("{0}ms: Async write started {1}ms after enqueue for {2}", swio.ElapsedMilliseconds.ToString().PadLeft(4), DateTime.UtcNow.Subtract(w.JobCreatedAt).Subtract(swio.Elapsed).TotalMilliseconds, result.RelativePath);
                                    }

                                } catch (Exception ex) {
                                    if (lp.Logger != null) {
                                        lp.Logger.Error("Failed to flush async write, {0} {1}\n{2}",ex.ToString(), result.RelativePath,ex.StackTrace);
                                    }
                                } finally {
                                    CurrentWrites.Remove(job); //Remove from the queue, it's done or failed. 
                                }

                            })) {
                                //We queued it! Send back a read-only memory stream
                                result.Data = w.GetReadonlyStream();
                            } else {
                                asyncFailed = false;
                                //We failed to queue it - either the ThreadPool was exhausted or we exceeded the MB limit for the write queue.
                                //Write the MemoryStream to disk using the normal method.
                                //This is nested inside a queuelock because if we failed here, the next one will also. Better to force it to wait until the file is written to disk.
                                if (!await TryWriteFile(result, physicalPath, relativePath, async delegate(Stream s) { await ms.CopyToAsync(s); }, timeoutMs, false)) {
                                    if (lp.Logger != null)
                                        lp.Logger.Warn("Failed to queue async write, also failed to lock for sync writing: {0}", result.RelativePath);
                                        
                                }
                            }

                        }

                    })) {
                    //On failure
                    result.Result = CacheQueryResult.Failed;
                }

            }
            if (lp.Logger != null) {
                sw.Stop();
                lp.Logger.Trace("{0}ms: {3}{1} for {2}, Key: {4}", sw.ElapsedMilliseconds.ToString(NumberFormatInfo.InvariantInfo).PadLeft(4), result.Result.ToString(), result.RelativePath, asynchronous ? (asyncFailed ? "AsyncHttpMode, fell back to sync write  " : "AsyncHttpMode+AsyncWrites ") : "AsyncHttpMode", keyBasis);
            }
            //Fire event
            if (CacheResultReturned != null) CacheResultReturned(this, result);
            return result;
        }


        /// <summary>
        /// Returns true if either (a) the file was written, or (b) the file already existed with a matching modified date.
        /// Returns false if the in-process lock failed. Throws an exception if any kind of file or processing exception occurs.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="physicalPath"></param>
        /// <param name="relativePath"></param>
        /// <param name="writeCallback"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="recheckFS"></param>
        /// <returns></returns>
        private async Task<bool> TryWriteFile(CacheResult result, string physicalPath, string relativePath, AsyncWriteResult writeCallback, int timeoutMs, bool recheckFS)
        {

            
            bool miss = true;
            if (recheckFS) {
                miss = !Index.existsCertain(relativePath, physicalPath);
                if (!miss && !Locks.MayBeLocked(relativePath.ToUpperInvariant())) return true;
            }
               

            //Lock execution using relativePath as the sync basis. Ignore casing differences. This locking is process-local, but we also have code to handle file locking.
            return await ((AsyncLockProvider)Locks).TryExecuteAsync(relativePath.ToUpperInvariant(), timeoutMs,
                async delegate() {

                    //On the second check, use cached data for speed. The cached data should be updated if another thread updated a file (but not if another process did).
                    if (!Index.exists(relativePath, physicalPath)) {

                        //Create subdirectory if needed.
                        if (!Directory.Exists(Path.GetDirectoryName(physicalPath))) {
                            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath));
                            if (lp.Logger != null) lp.Logger.Debug("Creating missing parent directory {0}", Path.GetDirectoryName(physicalPath));
                        }

                        //Open stream 
                        //Catch IOException, and if it is a file lock,
                        // - (and hashmodified is true), then it's another process writing to the file, and we can serve the file afterwards
                        // - (and hashmodified is false), then it could either be an IIS read lock or another process writing to the file. Correct behavior is to kill the request here, as we can't guarantee accurate image data.
                        // I.e, hashmodified=true is the only supported setting for multi-process environments.
                        //TODO: Catch UnathorizedAccessException and log issue about file permissions.
                        //... If we can wait for a read handle for a specified timeout.

                        IOException locked_exception = null;

                        try
                        {
                            string tempFile = physicalPath + ".tmp_" + new Random().Next(int.MaxValue).ToString("x") + ".tmp";

                            System.IO.FileStream fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                            bool finished = false;
                            try
                            {
                                using (fs)
                                {
                                    //Run callback to write the cached data
                                    await writeCallback(fs); //Can throw any number of exceptions.
                                    await fs.FlushAsync();
                                    fs.Flush(true);
                                    finished = true;
                                }
                            }
                            finally
                            {
                                //Don't leave half-written files around.
                                if (!finished)
                                {
                                    try { if (File.Exists(tempFile)) File.Delete(tempFile); }
                                    catch { }
                                }
                            }
                            bool moved = false;
                            if (finished)
                            {
                                try
                                {
                                    File.Move(tempFile, physicalPath);
                                    moved = true;
                                }
                                catch (IOException)
                                {
                                    //Will throw IO exception if already exists. Which we consider a hit, so we delete the tempFile
                                    try { if (File.Exists(tempFile)) File.Delete(tempFile); }
                                    catch { }
                                }
                            }
                            if (moved)
                            {
                                DateTime createdUtc = DateTime.UtcNow;
                                //Set the created date, so we know the last time we updated the cache.s
                                System.IO.File.SetCreationTimeUtc(physicalPath, createdUtc);
                                //Update index
                                //TODO: what should sourceModifiedUtc be when there is no modified date?
                                Index.setCachedFileInfo(relativePath, new CachedFileInfo(createdUtc, createdUtc, createdUtc));
                                //This was a cache miss
                                if (result != null) result.Result = CacheQueryResult.Miss;
                            }
                        }
                        catch (IOException ex)
                        {

                            if (IsFileLocked(ex)) locked_exception = ex;
                             else throw;
                        }
                        if (locked_exception != null)
                        {
                            //Somehow in between verifying the file didn't exist and trying to create it, the file was created and locked by someone else.
                            //When hashModifiedDate==true, we don't care what the file contains, we just want it to exist. If the file is available for 
                            //reading within timeoutMs, simply do nothing and let the file be returned as a hit.
                            Stopwatch waitForFile = new Stopwatch();
                            bool opened = false;
                            while (!opened && waitForFile.ElapsedMilliseconds < timeoutMs)
                            {
                                waitForFile.Start();
                                bool waitABitMore = false;
                                try
                                {
                                    using (FileStream temp = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                        opened = true;
                                }
                                catch (IOException iex)
                                {
                                    if (IsFileLocked(iex))
                                        waitABitMore = true;

                                    else throw iex;
                                }
                                if (waitABitMore) { await Task.Delay((int)Math.Min(30, Math.Round((float)timeoutMs / 3.0))); }
                                waitForFile.Stop();
                            }
                            if (!opened) throw locked_exception; //By not throwing an exception, it is considered a hit by the rest of the code.

                        }


                    }
                });
        }

        private static bool IsFileLocked(IOException exception) {
            int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }


    }
}
