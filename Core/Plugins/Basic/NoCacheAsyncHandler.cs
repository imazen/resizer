/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ImageResizer.Caching;
using System.Threading.Tasks;
using System.Threading;

namespace ImageResizer.Plugins.Basic
{
    /// <summary>
    /// Implements IHttpHandler, serves content for the NoCache plugin
    /// </summary>
    public class NoCacheAsyncHandler : IHttpAsyncHandler
    {
        private IAsyncResponsePlan e;

        public NoCacheAsyncHandler(IAsyncResponsePlan e)
        {
            this.e = e;
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            throw new NotSupportedException();
        }

        protected async Task ProcessRequestAsync(HttpContext context){
            context.Response.StatusCode = 200;
            context.Response.BufferOutput = true; //Same as .Buffer. Allows bitmaps to be disposed quicker.
            context.Response.ContentType = e.EstimatedContentType;
            await e.CreateAndWriteResultAsync(context.Response.OutputStream, e);
        }
    
        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
 	        Task task = ProcessRequestAsync(context);
            if (task == null)
            {
                return null;
            }

            var retVal = new TaskWrapperAsyncResult(task, extraData);

            if (cb != null)
            {
                // The callback needs the same argument that the Begin method returns, which is our special wrapper, not the original Task.
                task.ContinueWith(_ => cb(retVal));
            }

            return retVal;
        }

        public void EndProcessRequest(IAsyncResult result)
        {
 	        if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            // The End* method doesn't actually perform any actual work, but we do need to maintain two invariants:
            // 1. Make sure the underlying Task actually *is* complete.
            // 2. If the Task encountered an exception, observe it here.
            // (The Wait method handles both of those.)
            var castResult = (TaskWrapperAsyncResult)result;
            castResult.Task.Wait();
        }

        // Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.


        /// <summary>
        /// Wraps a <see cref="Task"/>, optionally overriding the State object (since the Task Asynchronous Pattern doesn't normally use it).
        /// </summary>
        /// <remarks>Class copied from System.Web.Mvc, but with modifications</remarks>
        internal sealed class TaskWrapperAsyncResult : IAsyncResult
        {
            private bool? _completedSynchronously;

            /// <summary>
            /// Initializes a new instance of the <see cref="TaskWrapperAsyncResult"/> class.
            /// </summary>
            /// <param name="task">The <see cref="Task"/> to wrap.</param>
            /// <param name="asyncState">User-defined object that qualifies or contains information about an asynchronous operation.</param>
            public TaskWrapperAsyncResult(Task task, object asyncState)
            {
                if (task == null)
                {
                    throw new ArgumentNullException("task");
                }

                Task = task;
                AsyncState = asyncState;
            }

            /// <summary>
            /// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
            /// </summary>
            /// <returns>A user-defined object that qualifies or contains information about an asynchronous operation.</returns>
            public object AsyncState { get; private set; }

            /// <summary>
            /// Gets a <see cref="T:System.Threading.WaitHandle"/> that is used to wait for an asynchronous operation to complete.
            /// </summary>
            /// <returns>A <see cref="T:System.Threading.WaitHandle"/> that is used to wait for an asynchronous operation to complete.</returns>
            public WaitHandle AsyncWaitHandle
            {
                get { return ((IAsyncResult)Task).AsyncWaitHandle; }
            }

            /// <summary>
            /// Gets a value indicating whether the asynchronous operation completed synchronously.
            /// </summary>
            /// <returns>true if the asynchronous operation completed synchronously; otherwise, false.</returns>
            public bool CompletedSynchronously
            {
                get { return _completedSynchronously ?? ((IAsyncResult)Task).CompletedSynchronously; }
                internal set { _completedSynchronously = value; }
            }

            /// <summary>
            /// Gets a value indicating whether the asynchronous operation has completed.
            /// </summary>
            /// <returns>true if the operation is complete; otherwise, false.</returns>
            public bool IsCompleted
            {
                get { return ((IAsyncResult)Task).IsCompleted; }
            }

            /// <summary>
            /// Gets the task.
            /// </summary>
            public Task Task { get; private set; }
        }

    }
}
