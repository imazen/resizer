/* Copyright (c) 2014 Imazen See license.txt for your rights */

using System;
using System.Threading;
using System.Collections.Generic;
using Ionic.Zip;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace ImageResizer.Plugins.BatchZipper
{
    public class ItemEventArgs : EventArgs
    {
        public ItemEventArgs(Guid jobId, ItemResult result)
        {
            this.jobId = jobId; this.result = result;
        }

        public ItemEventArgs(Guid jobId, ItemResult result, JobStats jobStats)
        {
            // TODO: Complete member initialization
            this.jobId = jobId;
            this.result = result;
            this.jobStats = jobStats;
        }
        private Guid jobId;
        public Guid JobId { get { return jobId; } }
        private ItemResult result;
        public ItemResult Result { get { return result; } }

        private bool cancel = false;
        /// <summary>
        /// Set to true to cancel the job. Defaults to false for all events.
        /// </summary>
        public bool Cancel { get { return cancel; } set { cancel = value; } }

        private JobStats jobStats;
        /// <summary>
        /// A snapshot of job statistics
        /// </summary>
        public JobStats Stats { get { return jobStats; } }


        public override string ToString()
        {
            string s =  base.ToString();
            if (jobId != null) s += " for job " + jobId.ToString();
            if (result != null) s+= "\n" + result.ToString();
            if (Stats != null) s += "\n" + Stats.ToString();
            return s;
        }

    }
    public class JobEventArgs : EventArgs
    {
        public JobEventArgs(Guid jobId, JobResult result)
        {
            this.jobId = jobId; this.result = result;
        }
        private Guid jobId;
        public Guid JobId { get { return jobId; } }
        private JobResult result;
        public JobResult Result { get { return result; } }

        public override string ToString()
        {
            string s = base.ToString();
            if (jobId != null) s += " for job " + jobId.ToString();
            if (result != null) s += "\n" + result.ToString();
            return s;
        }
    }

    /// <summary>
    /// Not always the execption you'll see in the JobEvent handler when you cancel a job.
    /// </summary>
    [Serializable()]
    public class JobCancelledException : System.ApplicationException
    {
        public JobCancelledException() : base() { }
        public JobCancelledException(string message) : base(message) { }
        public JobCancelledException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected JobCancelledException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }


    /// <summary>
    /// Holds the result of a items resize and/or compression attempt.
    /// </summary>
    public class ItemResult
    {
        private BatchResizeItem item;
        private bool successful;
        private Exception itemError;

        public ItemResult(BatchResizeItem item, bool successful, Exception itemError)
        {
            this.item = item; this.successful = successful; this.itemError = itemError;
        }
        /// <summary>
        /// True if the item was successfully copied into the zip file.
        /// </summary>
        public bool Successful { get { return successful; } }
        public BatchResizeItem Item { get { return item; } }
        /// <summary>
        /// The exception that occured when resizing, opening, copying, or compressing the file.
        /// </summary>
        public Exception ItemError { get { return itemError; } }

        public override string ToString()
        {
            string s = base.ToString() + " ";
            if (item != null) s += item.ToString() + "\n";
            s += "success=" + Successful.ToString();
            if (itemError != null)
            {
                s += "\n" + itemError.ToString() + "\n" + itemError.StackTrace;
            }
            return s;
        }
    }

    /// <summary>
    /// Gives the result: success or failure to create a .zip. Also gives a snapshot of the job's statistics.
    /// </summary>
    public class JobResult
    {
        private bool successful;
        /// <summary>
        /// True if the job was completed successfully. Doesn't mean there were not errors on individual files, check ItemResults or Stats for that information.
        /// Just means there should be a .zip waiting.
        /// </summary>
        public bool Successful { get { return successful; } }

        private List<ItemResult> itemResults;
        /// <summary>
        /// The individual results for each of the items requested.
        /// </summary>
        public List<ItemResult> ItemResults { get { return itemResults; } }

        private Exception jobError;
        /// <summary>
        /// The exception that occured while processing the job. 
        /// </summary>
        public Exception JobError { get { return jobError; } }
        private JobStats jobStats;
        /// <summary>
        /// A snapshot of job statistics
        /// </summary>
        public JobStats Stats { get { return jobStats; } }
        public JobResult(List<ItemResult> itemResults, bool successful, Exception jobError, JobStats stats)
        {
            this.itemResults = itemResults;
            this.successful = successful;
            this.jobError = jobError;
            this.jobStats = stats;
        }

        public override string ToString()
        {
            string s = base.ToString() + " ";
            s += "success=" + Successful.ToString();
            if (JobError != null) s += "\n" + JobError.ToString() + "\n" + JobError.StackTrace;
            if (Stats != null) s += "\n" + Stats.ToString();
            return s;
        }
    }

    public class JobStats
    {
        public JobStats(int requestedItems, int successfulItems, int failedItems, long executionTime)
        {
            this.requestedItems = requestedItems; this.successfulItems = successfulItems; this.failedItems = failedItems; this.executionTime = executionTime;
        }
        protected int requestedItems, successfulItems, failedItems;
        protected long executionTime;
        /// <summary>
        /// The number of items specified in the job description
        /// </summary>
        public int RequestedItems { get { return requestedItems; } }
        /// <summary>
        /// The number of items successfully added to the zip file
        /// </summary>
        public int SuccessfulItems { get { return successfulItems; } }
        /// <summary>
        /// The number of items that failed to be added.
        /// </summary>
        public int FailedItems { get { return failedItems; } }
        /// <summary>
        /// The number of milliseconds that have elapsed since the job began.
        /// </summary>
        public long ExecutionTime { get { return executionTime; } }

        public override string ToString()
        {
            return base.ToString() + "{Requested=" + RequestedItems + ", Successful=" + successfulItems + ", Failed=" + failedItems + ", ExecutionTime=" + ExecutionTime + "ms}";
        }
    }
    
}