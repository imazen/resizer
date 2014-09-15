/* Copyright (c) 2014 Imazen See license.txt for your rights */

using System;
using System.Threading;
using System.Collections.Generic;
using Ionic.Zip;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

namespace ImageResizer.Plugins.BatchZipper
{
    /// <summary>
    /// Item Event arguments for item events
    /// </summary>
    public class ItemEventArgs : EventArgs
    {
        /// <summary>
        /// Initialize ItemEventArgs with job and result
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="result"></param>
        public ItemEventArgs(Guid jobId, ItemResult result)
        {
            this.jobId = jobId; this.result = result;
        }

        /// <summary>
        /// Initialize ItemEventArgs with job, result, and status
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="result"></param>
        /// <param name="jobStats"></param>
        public ItemEventArgs(Guid jobId, ItemResult result, JobStats jobStats)
        {
            // TODO: Complete member initialization
            this.jobId = jobId;
            this.result = result;
            this.jobStats = jobStats;
        }
        private Guid jobId;

        /// <summary>
        /// gets the current jobId
        /// </summary>
        public Guid JobId { get { return jobId; } }
        private ItemResult result;

        /// <summary>
        /// gets the current Item Result
        /// </summary>
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

        /// <summary>
        /// return job, result, and status
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string s =  base.ToString();
            if (jobId != null) s += " for job " + jobId.ToString();
            if (result != null) s+= "\n" + result.ToString();
            if (Stats != null) s += "\n" + Stats.ToString();
            return s;
        }

    }

    /// <summary>
    /// Job event args for job events
    /// </summary>
    public class JobEventArgs : EventArgs
    {
        /// <summary>
        /// Initialize the JobEventArgs with an Id and result
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="result"></param>
        public JobEventArgs(Guid jobId, JobResult result)
        {
            this.jobId = jobId; this.result = result;
        }
        private Guid jobId;

        /// <summary>
        /// Gets unique job id
        /// </summary>
        public Guid JobId { get { return jobId; } }
        private JobResult result;

        /// <summary>
        /// Gets unique job result
        /// </summary>
        public JobResult Result { get { return result; } }

        /// <summary>
        /// Return job id and result
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Initialize the JobCancelledException
        /// </summary>
        public JobCancelledException() : base() { }

        /// <summary>
        /// Initialize the JobCancelledException with a given message
        /// </summary>
        public JobCancelledException(string message) : base(message) { }

        /// <summary>
        /// Initialize the JobCancelledException with a given message and inner exception
        /// </summary>
        public JobCancelledException(string message, System.Exception inner) : base(message, inner) { }

        /// <summary>
        /// A constructor is needed for serialization when an
        /// exception propagates from a remoting server to the client. 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
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

        /// <summary>
        /// Initialize the ItemResult with the item, result, and exception
        /// </summary>
        /// <param name="item"></param>
        /// <param name="successful"></param>
        /// <param name="itemError"></param>
        public ItemResult(BatchResizeItem item, bool successful, Exception itemError)
        {
            this.item = item; this.successful = successful; this.itemError = itemError;
        }
        /// <summary>
        /// True if the item was successfully copied into the zip file.
        /// </summary>
        public bool Successful { get { return successful; } }

        /// <summary>
        /// Get Item to batch resize
        /// </summary>
        public BatchResizeItem Item { get { return item; } }
        /// <summary>
        /// The exception that occured when resizing, opening, copying, or compressing the file.
        /// </summary>
        public Exception ItemError { get { return itemError; } }

        /// <summary>
        /// Return the error and stack trace
        /// </summary>
        /// <returns></returns>
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
    /// Results of the batch job
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

        /// <summary>
        /// Initialize the JOb result with specific given values
        /// </summary>
        /// <param name="itemResults"></param>
        /// <param name="successful"></param>
        /// <param name="jobError"></param>
        /// <param name="stats"></param>
        public JobResult(List<ItemResult> itemResults, bool successful, Exception jobError, JobStats stats)
        {
            this.itemResults = itemResults;
            this.successful = successful;
            this.jobError = jobError;
            this.jobStats = stats;
        }

        /// <summary>
        /// Return erros and statistic from teh batch job
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string s = base.ToString() + " ";
            s += "success=" + Successful.ToString();
            if (JobError != null) s += "\n" + JobError.ToString() + "\n" + JobError.StackTrace;
            if (Stats != null) s += "\n" + Stats.ToString();
            return s;
        }
    }

    /// <summary>
    /// Batch job statistics
    /// </summary>
    public class JobStats
    {
        /// <summary>
        /// Initialize the JobStats with given values
        /// </summary>
        /// <param name="requestedItems"></param>
        /// <param name="successfulItems"></param>
        /// <param name="failedItems"></param>
        /// <param name="executionTime"></param>
        public JobStats(int requestedItems, int successfulItems, int failedItems, long executionTime)
        {
            this.requestedItems = requestedItems; this.successfulItems = successfulItems; this.failedItems = failedItems; this.executionTime = executionTime;
        }
        /// <summary>
        /// How many Requested Items, Successful Items, and Failed Items
        /// </summary>
        protected int requestedItems, successfulItems, failedItems;

        /// <summary>
        /// Time for for Batch job to execute
        /// </summary>
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

        /// <summary>
        /// returns requested, successfull, and failed items along with execution time
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString() + "{Requested=" + RequestedItems + ", Successful=" + successfulItems + ", Failed=" + failedItems + ", ExecutionTime=" + ExecutionTime + "ms}";
        }
    }
    
}