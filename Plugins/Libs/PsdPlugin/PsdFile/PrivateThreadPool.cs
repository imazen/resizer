/* Copyright (c) 2014 Imazen See license.txt for your rights */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Runtime.InteropServices;

namespace PhotoshopFile
{
    public interface IIsDisposed : IDisposable
    {
        // Properties
        bool IsDisposed { get; }
    }


    [Serializable]
    public abstract class Disposable : IIsDisposed, IDisposable
    {
        // Fields
        private bool isDisposed;

        // Methods
        protected Disposable()
        {
        }

        public static IDisposable Combine(IEnumerable<IDisposable> list)
        {
            IDisposable[] listArray = list.ToArray<IDisposable>();
            return FromAction(delegate
            {
                IDisposable[] listArrayP = Interlocked.Exchange<IDisposable[]>(ref listArray, null);
                if (listArrayP != null)
                {
                    foreach (IDisposable disposable in listArrayP)
                    {
                        disposable.Dispose();
                    }
                }
                listArrayP = null;
            });
        }

        public static IDisposable Combine(IDisposable first, IDisposable second)
        {
            IDisposable firstCopy = first;
            IDisposable secondCopy = second;
            return FromAction(delegate
            {
                IDisposable firstCopyP = Interlocked.Exchange<IDisposable>(ref firstCopy, null);
                if (firstCopyP != null)
                {
                    firstCopyP.Dispose();
                    firstCopyP = null;
                }
                IDisposable secondCopyP = Interlocked.Exchange<IDisposable>(ref secondCopy, null);
                if (secondCopyP != null)
                {
                    secondCopyP.Dispose();
                    secondCopyP = null;
                }
            });
        }

        public void Dispose()
        {
            this.isDisposed = true;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        ~Disposable()
        {
            this.isDisposed = true;
            this.Dispose(false);
        }

        public static IDisposable FromAction(Action action)
        {
            return new ActionDisposable(action);
        }

        // Properties
        public bool IsDisposed
        {
            get
            {
                return this.isDisposed;
            }
        }

        // Nested Types
        private sealed class ActionDisposable : Disposable
        {
            // Fields
            private Action action;

            // Methods
            public ActionDisposable(Action action)
            {
                this.action = action;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Action actionP = Interlocked.Exchange<Action>(ref this.action, null);
                    if (actionP != null)
                    {
                        actionP();
                    }
                }
                base.Dispose(disposing);
            }
        }
    }

    public class WorkerThreadException : Exception
    {
        // Fields
        private const string defaultMessage = "Worker thread threw an exception";

        // Methods
        public WorkerThreadException(Exception innerException)
            : this("Worker thread threw an exception", innerException)
        {
        }

        public WorkerThreadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }



    public sealed class PrivateThreadPool : Disposable
    {
        // Fields
        private WaitableCounter counter;
        private ArrayList exceptions;
        private static PrivateThreadPool global = new PrivateThreadPool(Math.Min(WaitableCounter.MaximumCount, 2 * Environment.ProcessorCount));
        private bool useFXTheadPool;

        // Methods
        public PrivateThreadPool()
            : this(Environment.ProcessorCount)
        {
        }

        public PrivateThreadPool(int maxThreads)
            : this(maxThreads, true)
        {
        }

        public PrivateThreadPool(int maxThreads, bool useFXThreadPool)
        {
            this.exceptions = ArrayList.Synchronized(new ArrayList());
            if ((maxThreads < MinimumCount) || (maxThreads > MaximumCount))
            {
                throw new ArgumentOutOfRangeException("maxThreads", "must be between " + MinimumCount.ToString() + " and " + MaximumCount.ToString() + " inclusive");
            }
            this.counter = new WaitableCounter(maxThreads);
            this.useFXTheadPool = useFXThreadPool;
        }

        private void ClearExceptions()
        {
            this.exceptions.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Drain();
                if (this.counter != null)
                {
                    this.counter.Dispose();
                    this.counter = null;
                }
            }
            base.Dispose(disposing);
        }

        public void Drain()
        {
            this.counter.WaitForEmpty();
            this.DrainExceptions();
        }

        private void DrainExceptions()
        {
            if (this.exceptions.Count > 0)
            {
                throw new WorkerThreadException("Worker thread threw an exception", (Exception)this.exceptions[0]);
            }
            this.ClearExceptions();
        }

        public void QueueUserWorkItem(WaitCallback callback)
        {
            this.QueueUserWorkItem(callback, null);
        }

        public void QueueUserWorkItem(WaitCallback callback, object state)
        {
            IDisposable token = this.counter.AcquireToken();
            ThreadWrapperContext twc = new ThreadWrapperContext(callback, state, token, this.exceptions);
            if (this.counter.Max == 1)
            {
                twc.ThreadWrapper(twc);
            }
            else if (this.useFXTheadPool)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(twc.ThreadWrapper), twc);
            }
            else
            {
                Thread thread = new Thread(new ThreadStart(twc.ThreadWrapper));
                thread.IsBackground = true;
                thread.Start();
            }
        }

        // Properties
        private Exception[] Exceptions
        {
            get
            {
                return (Exception[])this.exceptions.ToArray(typeof(Exception));
            }
        }

        public static PrivateThreadPool Global
        {
            get
            {
                return global;
            }
        }

        public static int MaximumCount
        {
            get
            {
                return WaitableCounter.MaximumCount;
            }
        }

        public static int MinimumCount
        {
            get
            {
                return WaitableCounter.MinimumCount;
            }
        }

        public int Threads
        {
            get
            {
                return this.counter.Max;
            }
        }

        // Nested Types
        private sealed class ThreadWrapperContext
        {
            // Fields
            private WaitCallback callback;
            private object context;
            private IDisposable counterToken;
            private ArrayList exceptionsBucket;

            // Methods
            public ThreadWrapperContext(WaitCallback callback, object context, IDisposable counterToken, ArrayList exceptionsBucket)
            {
                this.callback = callback;
                this.context = context;
                this.counterToken = counterToken;
                this.exceptionsBucket = exceptionsBucket;
            }

            public void ThreadWrapper()
            {
                IDisposable token = this.counterToken;
                try
                {
                    this.callback(this.context);
                }
                catch (Exception ex)
                {
                    this.exceptionsBucket.Add(ex);
                }
                finally
                {
                    if (token != null)
                    {
                        token.Dispose();
                    }
                }
            }

            public void ThreadWrapper(object state)
            {
                this.ThreadWrapper();
            }
        }
    }

    public interface IWaitableCounter : IDisposable
    {
        // Methods
        IDisposable AcquireToken();
        bool IsEmpty();
        void WaitForEmpty();
        void WaitForNotFull();
    }

 

 


    public sealed class WaitableCounter : Disposable, IWaitableCounter, IDisposable
    {
        // Fields
        private WaitHandleArray freeEvents;
        private WaitHandleArray inUseEvents;
        private object theLock;

        // Methods
        public WaitableCounter(int maxCount)
        {
            if ((maxCount < 1) || (maxCount > 0x40))
            {
                throw new ArgumentOutOfRangeException("maxCount", "must be between 1 and 64, inclusive");
            }
            this.freeEvents = new WaitHandleArray(maxCount);
            this.inUseEvents = new WaitHandleArray(maxCount);
            for (int i = 0; i < maxCount; i++)
            {
                this.freeEvents[i] = new ManualResetEvent(true);
                this.inUseEvents[i] = new ManualResetEvent(false);
            }
            this.theLock = new object();
        }

        public IDisposable AcquireToken()
        {
            lock (this.theLock)
            {
                int index = this.WaitForNotFull2();
                ((ManualResetEvent)this.freeEvents[index]).Reset();
                ((ManualResetEvent)this.inUseEvents[index]).Set();
                return new CounterToken(this, index);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.freeEvents != null)
                {
                    for (int i = 0; i < this.freeEvents.Length; i++)
                    {
                        WaitHandle freeEvent = this.freeEvents[i];
                        if (freeEvent != null)
                        {
                            freeEvent.Close();
                        }
                    }
                    this.freeEvents = null;
                }
                if (this.inUseEvents != null)
                {
                    for (int i = 0; i < this.inUseEvents.Length; i++)
                    {
                        WaitHandle inUseEvent = this.inUseEvents[i];
                        if (inUseEvent != null)
                        {
                            inUseEvent.Close();
                        }
                    }
                    this.inUseEvents = null;
                }
            }
            base.Dispose(disposing);
        }

        public bool IsEmpty()
        {
            return this.freeEvents.AreAllSignaled();
        }

        private void Release(CounterToken token)
        {
            ((ManualResetEvent)this.inUseEvents[token.Index]).Reset();
            ((ManualResetEvent)this.freeEvents[token.Index]).Set();
        }

        public void WaitForEmpty()
        {
            this.freeEvents.WaitAll();
        }

        public void WaitForNotFull()
        {
            this.WaitForNotFull2();
        }

        private int WaitForNotFull2()
        {
            return this.freeEvents.WaitAny();
        }

        // Properties
        public int Max
        {
            get
            {
                return this.freeEvents.Length;
            }
        }

        public static int MaximumCount
        {
            get
            {
                return 0x40;
            }
        }

        public static int MinimumCount
        {
            get
            {
                return 1;
            }
        }

        // Nested Types
        private sealed class CounterToken : IDisposable
        {
            // Fields
            private int index;
            private WaitableCounter parent;

            // Methods
            public CounterToken(WaitableCounter parent, int index)
            {
                this.parent = parent;
                this.index = index;
            }

            public void Dispose()
            {
                this.parent.Release(this);
            }

            // Properties
            public int Index
            {
                get
                {
                    return this.index;
                }
            }
        }
    }


    public sealed class WaitHandleArray
    {
        // Fields
        public const int MaximumCount = 0x40;
        public const int MinimumCount = 1;
        private IntPtr[] nativeHandles;
        private WaitHandle[] waitHandles;

        // Methods
        public WaitHandleArray(int count)
        {
            if ((count < 1) || (count > 0x40))
            {
                throw new ArgumentOutOfRangeException("count", "must be between 1 and 64, inclusive");
            }
            this.waitHandles = new WaitHandle[count];
            this.nativeHandles = new IntPtr[count];
        }

        public bool AreAllSignaled()
        {
            return this.AreAllSignaled(0);
        }

        public bool AreAllSignaled(uint msTimeout)
        {
            uint result = this.WaitForAll(msTimeout);
            return ((result >= 0) && (result < this.Length));
        }

        public void WaitAll()
        {
            this.WaitForAll(uint.MaxValue);
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForMultipleObjects(uint nCount, IntPtr[] lpHandles, [MarshalAs(UnmanagedType.Bool)] bool bWaitAll, uint dwMilliseconds);

        public static uint WaitForMultipleObjects(IntPtr[] lpHandles, bool bWaitAll, uint dwMilliseconds)
        {
            return WaitForMultipleObjects((uint)lpHandles.Length, lpHandles, bWaitAll, dwMilliseconds);
        }

 
        public int WaitAny()
        {
            return (int)WaitForMultipleObjects(this.nativeHandles, false, uint.MaxValue);
        }

        private uint WaitForAll(uint dwTimeout)
        {
            return WaitForMultipleObjects(this.nativeHandles, true, dwTimeout);
        }

        // Properties
        public WaitHandle this[int index]
        {
            get
            {
                return this.waitHandles[index];
            }
            set
            {
                this.waitHandles[index] = value;
                this.nativeHandles[index] = value.SafeWaitHandle.DangerousGetHandle();
            }
        }

        public int Length
        {
            get
            {
                return this.waitHandles.Length;
            }
        }
    }


}
