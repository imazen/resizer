/* Copyright (c) 2014 Imazen See license.txt for your rights */

using System;
using System.Threading;
using System.Collections.Generic;
using Ionic.Zip;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Diagnostics;
using ImageResizer.Configuration;
using ImageResizer;
using System.IO;
using ImageResizer.Util;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.BatchZipper
{

    /// <summary>
    /// Synchronous worker class, not thread safe. Should be executed and touched by a single thread.
    /// </summary>
    public class BatchResizeWorker
    {
        /// <summary>
        /// Creates a new worker instance. Do not modify the settings while executing the Work() method.
        /// </summary>
        /// <param name="s"></param>
        public BatchResizeWorker(BatchResizeSettings s){
            this.s = s;
        }
        /// <summary>
        /// Stores the job settings. 
        /// </summary>
        protected BatchResizeSettings s;
        /// <summary>
        /// Used to map ZIP item names to instances. Required do to DotNetZipLib's callback system.
        /// </summary>
        protected Dictionary<string,BatchResizeItem> items;
        /// <summary>
        /// Used to store the results of each resized item.
        /// </summary>
        protected List<ItemResult> results = null;
        protected Stopwatch jobTimer;
        /// <summary>
        /// When the job was started
        /// </summary>
        protected DateTime startedAt = DateTime.MaxValue;

        /// <summary>
        /// When the job was finished
        /// </summary>
        protected DateTime finishedAt = DateTime.MaxValue;
        /// <summary>
        /// Executes the job, and returns when it is complete or has failed.
        /// Exceptions are delivered by events registered in the BatchResizeSetting instance - 
        /// throws no execptions, unless you throw an execption inside a job event handler.
        /// </summary>
        public void Work(){
            Work(s);
        }

        /// <summary>
        /// Not for external use. Assumes 'state' is a BatchResizeSettings instance, and stores in in a class member.
        /// Next, it completes (or fails at) the job. Throws exceptions only if (a) state is not a BatchResizeSettings instance, or (b) an exception is thrown by and handler of the JobEvent (Failed) event.
        /// </summary>
        /// <param name="state"></param>
        protected internal void Work(object state)
        {
            s = (BatchResizeSettings)state;
            try
            {
                //Time the job.
                jobTimer = new Stopwatch();
                jobTimer.Start();

                //1) Eliminate duplicate target filenames, sanitize, and add appropriate extensions, then mark all items as immutable
                s.FixDuplicateFilenames();
                s.AppendFinalExtensions();
                s.MarkItemsImmutable();

                //2) Build dictionary based on target filenames.
                items = BuildDict(s.files);

                //3) Verify destination folder exists, otherwise create it. (early detection of security failures)
                CreateDestinationFolder();

                //4) Open zip file (writes to a temporary location, then moves the file when it is done)
                using (Ionic.Zip.ZipFile z = new Ionic.Zip.ZipFile(s.destinationFile))
                {
                    //Compress for speed.
                    z.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
                    //Fire events instead of throwing exceptions
                    z.ZipErrorAction = ZipErrorAction.InvokeErrorEvent;
                    z.ZipError +=new EventHandler<ZipErrorEventArgs>(z_ZipError);
                    z.SaveProgress +=new EventHandler<SaveProgressEventArgs>(z_SaveProgress);
                    //5) Create a list to store item resize/compression results
                    results = new List<ItemResult>();

                    
                    //6) Queue the files that will be included in the archive, specifying a callback for items
                    foreach (BatchResizeItem i in s.files){
                        try{
                            ZipEntry ze = z.AddEntry(i.TargetFilename, new WriteDelegate(WriteItemCallback));
                            //Don't try to compress files we have already resized, it won't help, just slow things down and possibly use more space.
                            if (wouldResize(i)) ze.CompressionMethod = CompressionMethod.None;
                            else
                            {
                                //Set the times
                                //ze.SetEntryTimes(System.IO.File.GetLastWriteTimeUtc(i.PhysicalPath),
                                //    System.IO.File.GetLastAccessTimeUtc(i.PhysicalPath),
                                //    System.IO.File.GetCreationTimeUtc(i.PhysicalPath));
                                //Nevermind, not desired behavior.
                            }
                        }catch (Exception ex){
                            ItemFailed(i,ex);
                        }
                    }
                    //7) Begin proccessing the queue.
                    //As each file is written to the archive, a callback is invoked which actually provides the resized stream. 
                    z.Save();
                    

                } //8) Close zip file
                
                jobTimer.Stop();

                //9) Fire the event that says "We're done!"
                JobCompleted(s);
            }
            catch (Exception ex)
            {
                //Admit we failed to whoever is listening.
                JobFailed(s,ex);
            }
        }
        /// <summary>
        /// Returns true if the specified item is going to be resized.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public bool wouldResize(BatchResizeItem i)
        {
            //Can we resize it?
            bool resize = s.conf.Pipeline.IsAcceptedImageType(i.PhysicalPath);
            //Are we doing anything to it?
            if (String.IsNullOrEmpty(i.ResizeQuerystring)) resize = false;
            return resize;
        }
        /// <summary>
        /// Called from ZipFile.Save as each queued entry is processed.
        /// </summary>
        /// <param name="entryName"></param>
        /// <param name="stream"></param>
        protected void WriteItemCallback(string entryName, System.IO.Stream stream)
        {
            BatchResizeItem i = items[entryName];
            if (wouldResize(i))
            {
                //Buffer in a memory stream to avoid stepping on CrcCalculatorStream's broken toes
                using (MemoryStream ms = new MemoryStream()) {
                    s.conf.CurrentImageBuilder.Build(i.PhysicalPath, ms, new ResizeSettings(i.ResizeQuerystring));
                    ms.Position = 0;
                    ms.CopyToStream(stream);
                }
                return; //We're done!
                
            }


            //For non-resizeable items, Just copy the stream.
            using (System.IO.FileStream s = System.IO.File.OpenRead(i.PhysicalPath)) CopyStreamTo(s, stream);
        }
        /// <summary>
        /// Copies a read stream to a write stream.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        public static void CopyStreamTo(System.IO.Stream src, System.IO.Stream dest)
        {
            StreamExtensions.CopyToStream(src, dest);
        }
        /// <summary>
        /// Stores the results of the Zip file saving betweent he time Saving_Completed fires, and we close the ZipFile instance and fire our own event
        /// </summary>
        protected SaveProgressEventArgs savingCompletedEventArgs = null;

        protected void  z_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Saving_Completed) savingCompletedEventArgs = e;
            if (e.EventType == ZipProgressEventType.Saving_AfterWriteEntry && e.CurrentEntry != null && e.CurrentEntry.IncludedInMostRecentSave) ItemCompleted(e);
        }
        protected void z_ZipError(object sender, ZipErrorEventArgs e)
        {
            //We can only deal with item errors here. We ignore job errors, they will throw an execption that bubbles up through Save()
            if (e.CurrentEntry != null) ItemFailed(e);
        }

        private void JobCompleted(BatchResizeSettings s)
        {
            JobEventArgs args = new JobEventArgs(s.jobId, new JobResult(results, true,null, GetJobStats()));
            s.FireJobEvent(args);
        }

        private void JobFailed(BatchResizeSettings s,Exception ex)
        {
            JobEventArgs args = new JobEventArgs(s.jobId, new JobResult(results,false, ex, GetJobStats()));
            s.FireJobEvent(args);
        }

        /// <summary>
        /// How many items have been successfully resized and zipped
        /// </summary>
        protected int successfulItems = 0;
        /// <summary>
        /// How many items failed to be resized and zipped
        /// </summary>
        protected int failedItems = 0;

        /// <summary>
        /// Returns a snapshots of the job statistics
        /// </summary>
        /// <returns></returns>
        public JobStats GetJobStats()
        {
            return new JobStats(items.Count, successfulItems, failedItems, jobTimer != null ? jobTimer.ElapsedMilliseconds : 0);
        }


        private void ItemCompleted(SaveProgressEventArgs e)
        {
            successfulItems++;

            BatchResizeItem i = items[e.CurrentEntry.FileName];

            //Fire off the event again..
            ItemEventArgs args = new ItemEventArgs(s.jobId, new ItemResult(i, true, null), GetJobStats());
            s.FireItemEvent(args);

            //Store for later
            results.Add(args.Result);

            //If an event handler wants to cancel the job, do so.
            if (args.Cancel) e.Cancel = true;
        }

        /// <summary>
        /// Called when an error occurs during Save() (Whitch executes WriteItemCallback)
        /// </summary>
        /// <param name="e"></param>
        private void ItemFailed(ZipErrorEventArgs e)
        {
            failedItems++;

            e.CurrentEntry.ZipErrorAction = ZipErrorAction.Skip; //Prevent the item from crashing the job.

            BatchResizeItem i = items[e.CurrentEntry.FileName];
            Exception ex = e.Exception;
            //Fire off the event again..
            ItemEventArgs args = new ItemEventArgs(s.jobId, new ItemResult(i,false, ex), GetJobStats());
            s.FireItemEvent(args);

            //Store for later
            results.Add(args.Result);

            //If an event handler wants to cancel the job, do so.
            if (args.Cancel) e.Cancel = true;
        }
        /// <summary>
        /// Called when ZipFile.AddEntry fails. (Most errors should NOT happen here).
        /// </summary>
        /// <param name="i"></param>
        /// <param name="ex"></param>
        private void ItemFailed(BatchResizeItem i, Exception ex)
        {
            failedItems++;

            //Fire off the event
            ItemEventArgs args = new ItemEventArgs(s.jobId, new ItemResult(i,false, ex), GetJobStats());
            s.FireItemEvent(args);

            //Store for later
            results.Add(args.Result);

            //If a cancel is requested, fire an execption for Work to catch.
            throw new JobCancelledException("An event handler requested that the job be cancelled. The event handler was sent notification " +
                "that an item (\"" + i.TargetFilename +"\") could not be added to the zip file.",ex);
        }



  
        /// <summary>
        /// If the parent folder of the destination archive is missing, this method will create it.
        /// </summary>
        protected void CreateDestinationFolder(){
            string dir = System.IO.Path.GetDirectoryName(s.destinationFile);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// Builds a dictionary from a collection of BatchResizeItems, using .targetFilename as the key.
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public Dictionary<string,BatchResizeItem> BuildDict(IEnumerable<BatchResizeItem> files){
            Dictionary<string,BatchResizeItem> d = new Dictionary<string,BatchResizeItem>(StringComparer.OrdinalIgnoreCase);
            foreach(BatchResizeItem i in files) d.Add(i.TargetFilename,i);
            return d;
        }

        
    }
    /// <summary>
    /// Handles the threading of a batch resize procedure. Use the static method to queue a resize on a thread pool thread
    /// </summary>
    public class BatchResizeManager
    {
        //Can initiate resizes, specifying a callback for error and progress reporting
        /// <summary>
        /// Begins a batch resize operation on a background thread. If the ASP.NET process recycles, or the server reboots, the process will be aborted.
        /// 
        /// </summary>
        /// <param name="s"></param>
        public static void BeginBatchResize(BatchResizeSettings s)
        {
            ThreadPool.QueueUserWorkItem(new BatchResizeWorker(s).Work, s);
        }
    }
}