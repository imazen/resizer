// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ImageResizer.Util
{
    public static class AsyncUtils
    {
        
        private static readonly TaskFactory _myTaskFactory = new
            TaskFactory(CancellationToken.None,
                        TaskCreationOptions.None,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return AsyncUtils._myTaskFactory
                .StartNew<Task<TResult>>(func)
                .Unwrap<TResult>()
                .GetAwaiter()
                .GetResult();
        }

        public static void RunSync(Func<Task> func)
        {
            AsyncUtils._myTaskFactory
                .StartNew<Task>(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

       

        /// <summary>
        /// Wraps a <see cref="Task"/>, optionally overriding the State object (since the Task Asynchronous Pattern doesn't normally use it).
        /// </summary>
        /// <remarks>Class copied from System.Web.Mvc, but with modifications</remarks>
        public class TaskWrapperAsyncResult : IAsyncResult
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
