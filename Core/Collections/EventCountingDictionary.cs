using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ImageResizer.Collections {
    /// <summary>
    /// Specifies counter granularity, memory limits, cleanup intervals, and threading optimization level.
    /// </summary>
    public class EventCountingStrategy{
        public EventCountingStrategy() {
            Threading = ThreadingPrecision.Fast;
            CounterGranularity = 8;
            MaxBytesUsed = 0;
            CleanupInterval = new TimeSpan(0, 0, 20); //Once per 20 seconds
        }

        public enum ThreadingPrecision {
            /// <summary>
            /// Much faster than accurate mode, but at the expense of extremely rare counter inaccuracies. Irrelevant in light of counter granularity, usually. 
            /// </summary>
            Fast,
            Accurate
        }
        /// <summary>
        /// Whether to optimize for performance or counter accuracy. Defaults to Fast
        /// </summary>
        public ThreadingPrecision Threading{get;set;}

        /// <summary>
        /// How granular to track the time. For example, with a granularity of 8 (default), and an 8-minute tracking duration, the counter will track the last 7-8 minutes (maximum 0-59.9 second time window variance). 
        /// </summary>
        public int CounterGranularity{get;set;}

        /// <summary>
        /// Specifies a hard limit on the number of bytes used for tracking purposes. Causes the smallest-valued items to get discarded first. Doesn't count key size. Set to 0 to disable (default)
        /// </summary>
        public int MaxBytesUsed {get;set;}

        /// <summary>
        /// Specifies how often to eliminate 0-valued items from the dictionary. Defaults to 20 seconds.
        /// </summary>
        public TimeSpan CleanupInterval{get;set;}

        /// <summary>
        /// The estimated size (in bytes) of a counter (excluding the key). Based on CounterGranularity
        /// </summary>
        public int EstimatedCounterSize { get { return CounterGranularity * 4 + 128; } }
    }

    /// <summary>
    /// Maintains a dictionary of keys to counters. Counters track how many events have occured during the last X minutes/seconds/ticks for the given item.
    /// Can enforce size limits and cleanup empty counters on an inverval.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EventCountingDictionary<T> {
        public EventCountingDictionary(IEqualityComparer<T> keyComparer, TimeSpan trackingDuration, EventCountingStrategy strategy) {
            this._trackingDuration = trackingDuration;
            this.granularity = strategy.CounterGranularity;
            precision = strategy.Threading;

            keysToCounters = new Dictionary<T, EventCounter>(keyComparer);
            countersToKeys = new Dictionary<EventCounter, T>();
            //Est size per item
            itemSize = strategy.EstimatedCounterSize;
            //Max number of items
            maxItems = strategy.MaxBytesUsed / itemSize;

            cleanupInterval = strategy.CleanupInterval.Ticks;
        }

        private TimeSpan _trackingDuration;
        /// <summary>
        /// The duration for which to track events. For example, 5 minutes will keep a rolling value of how many events have occured in the last 5 minutes
        /// </summary>
        public TimeSpan TrackingDuration {
            get { return _trackingDuration; }
        }
        private int granularity = 0;
        private int itemSize = 0;
        private int maxItems = 0;
        private long cleanupInterval = 0;
        private EventCountingStrategy.ThreadingPrecision precision;

        /// <summary>
        /// For incrementing and finding counters based on keys
        /// </summary>
        private Dictionary<T, EventCounter> keysToCounters;
        /// <summary>
        /// Purely for cleanup purposes. Allows fast removal of pairs based on the EventCounter instance.
        /// </summary>
        private Dictionary<EventCounter, T> countersToKeys;
        /// <summary>
        /// Lock for access to dictionaries
        /// </summary>
        private object syncLock = new object();
        /// <summary>
        /// Lock on EventCounter.sortValue members. Redundant with cleanupLock implemented.
        /// </summary>
        private object sortLock = new object();
        /// <summary>
        /// Lock to prevent concurrent cleanups from occuring
        /// </summary>
        private object cleanupLock = new object();

        private long lastStarted = 0;
        /// <summary>
        /// Lock to prevent duplicate work items from being scheduled.
        /// </summary>
        private object timerLock = new object();

        /// <summary>
        /// Starts cleanup on a thread pool thread if it hasn't been started within the desired interval. If cleanup starts and other cleanup is running, it cancels. The assumption is that one cleanup of either type is enough within the interval.
        /// </summary>
        public void PingCleanup() {
            lock (timerLock) {
                if (DateTime.Now.Ticks - lastStarted > cleanupInterval) {
                    
                    if (System.Threading.ThreadPool.QueueUserWorkItem(delegate(object o){
                        Cleanup(CleanupMode.Maintenance);
                    })) lastStarted = DateTime.Now.Ticks;
                }
            }
        }


        private enum CleanupMode { MakeRoom, Maintenance }
        /// <summary>
        /// Performs cleanup on the dictionaries in either MakeRoom or Maintenance mode. Returns true if the goal was achieved, false if the cleanup was canceled because another cleanup was executing conurrently.
        /// </summary>
        /// <param name="skipOnContention">Always set this to true</param>
        private bool Cleanup(CleanupMode mode) {
            if (mode == CleanupMode.MakeRoom && maxItems < 1) return true; //We don't perform minimal cleanups unless a ceiling is specified.
            
            //We have to weak lock method-level, because otherwise a background thread could be cleaning when GetOrAdd is called, and we could have a deadlock
            //With syncLock locked in GetOrAdd, waiting on cleanupLock, and Cleanup locked on cleanupLock, waiting on GetOrAdd.
            //If we didn't have any method-level lock, we'd waste resources with simultaneous cleanup runs.
            //sortLock is redundant with cleanupLock, but remains in case I decide to pullt the method level lock
            if (!Monitor.TryEnter(cleanupLock)) return false; //Failed to lock, another thread is cleaning.
            try {
                //In high precision, we lock the entire long-running process
                if (precision == EventCountingStrategy.ThreadingPrecision.Accurate) Monitor.Enter(syncLock);
                try {
                    EventCounter[] counters;
                    //In fast mode, only lock for the copy and delete. We can sort outside after taking a snapshot. 
                    //We wont remove newly added ones, but thats ok. 
                    lock (syncLock) {
                        if (mode == CleanupMode.MakeRoom && keysToCounters.Count < maxItems) return true; //Nothing to do, there is stil room
                        counters = new EventCounter[countersToKeys.Count];
                        countersToKeys.Keys.CopyTo(counters, 0); //Clone 
                    }
                    //Lock for sorting so we have synchronized access to a.sortValue
                    lock (sortLock) {
                        //Pause values
                        for (int i = 0; i < counters.Length; i++) {
                            counters[i].sortValue = counters[i].GetValue();
                        }
                        //Sort lowest counters to the top using quicksort
                        Array.Sort<EventCounter>(counters, delegate(EventCounter a, EventCounter b) {
                            return a.sortValue - b.sortValue;
                        });
                    }
                    //Go back into lock
                    lock (syncLock) {
                        int removedItems = 0;
                        int goal = maxItems / 10; //10% is our goal if we are removing minimally.
                        //Remove items
                        for (int i = 0; i < counters.Length; i++) {
                            EventCounter c = counters[i];
                            if (mode == CleanupMode.MakeRoom && removedItems >= goal) return true; //Done, we hit our goal!
                            if (mode == CleanupMode.Maintenance && c.sortValue > 0) return true; //Done, We hit the end of the zeros
                            if (mode == CleanupMode.Maintenance && c.GetValue() > 0) continue; //Skip counters that incremeted while we were working.
                            //Look up key
                            T key;
                            countersToKeys.TryGetValue(c, out key);
                            if (key.Equals(default(T))) continue; //Skip counters that have already been removed
                            //Remove counter
                            countersToKeys.Remove(c);
                            keysToCounters.Remove(key);
                            removedItems++;
                        }
                    }
                } finally {
                    if (precision == EventCountingStrategy.ThreadingPrecision.Accurate) Monitor.Exit(syncLock);
                }
            } finally {
                Monitor.Exit(cleanupLock);
            }
            return true;
        }

        private EventCounter GetOrAdd(T key) {
            EventCounter c;
            lock (syncLock) {
                keysToCounters.TryGetValue(key, out c);
                if (c == null){
                    if (maxItems > 0 && keysToCounters.Count >= maxItems) Cleanup(CleanupMode.MakeRoom);
                    c = new EventCounter(granularity,(int)_trackingDuration.Ticks / granularity);
                    keysToCounters.Add(key,c);
                    countersToKeys.Add(c,key);
                }
            }
            return c;
        }
        /// <summary>
        /// Increment the counter for the given item
        /// </summary>
        /// <param name="key"></param>
        public void Increment(T key) {
            EventCounter c = GetOrAdd(key);
            if (precision == EventCountingStrategy.ThreadingPrecision.Fast)
                c.Increment();
            else
                c.IncrementExact();
        }
        /// <summary>
        /// Calculate the counter value for the given item
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetValue(T key) {
            EventCounter c;
            lock (syncLock) {
                keysToCounters.TryGetValue(key, out c);
            }
            if (c == null) return 0;
            return c.GetValue();
        }


        

    }

    /// <summary>
    /// Maintains a rotating data structure to track events over a moving time period.
    /// </summary>
    public class EventCounter {
        private int[] data;
        private int arraySize;
        private int ticksPer;
        private long started;

        public EventCounter(TimeSpan trackingDuration, TimeSpan trackingPrecision)
            : this((int)(trackingDuration.Ticks / trackingPrecision.Ticks), (int)trackingPrecision.Ticks) {
        }

        public EventCounter(int arraySize, int ticksPerElement) {
            data = new int[arraySize];
            this.arraySize = arraySize;
            ticksPer = ticksPerElement;
            started = DateTime.Now.Ticks;
        }

        public void Increment(long ticks) {
            int index = (int)((ticks - started / ticksPer) % arraySize);
            data[index]++; // Peformance > Precision
            //For Precision, use Interlocked.Increment(ref data[index]);
        }
        public void IncrementExact(long ticks) {
            int index = (int)((ticks - started / ticksPer) % arraySize);
            Interlocked.Increment(ref data[index]);
        }
        public void Increment() { Increment(DateTime.Now.Ticks); }
        public void IncrementExact() { Increment(DateTime.Now.Ticks); }
        /// <summary>
        /// Returns the value of the counter
        /// </summary>
        /// <returns></returns>
        public int GetValue() {
            int sum = 0;
            for (int i = 0; i < arraySize; i++)
                sum += data[i];
            return sum;
        }
        /// <summary>
        /// Warning! Not synchronized or updated. Use must be externally synchromized and value set externally
        /// </summary>
        internal int sortValue;
    }
}
