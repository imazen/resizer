using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    struct TimeSlotResult
    {
        public TimeSlotResult(long result, long slotBeginTicks)
        {
            this.value = result;
            this.SlotBeginTicks = slotBeginTicks;
        }

        public long SlotBeginTicks { get; private set; }

        long value;
        public long? Value
        {
            get
            {
                return SlotBeginTicks > 0 ? value : (long?)null;
            }
        }

        public bool IsEmpty { get { return SlotBeginTicks == 0; } }

        public static readonly TimeSlotResult Empty = new TimeSlotResult();

        public override string ToString()
        {
            return IsEmpty ? "(empty)" : string.Format("[{0}] at {1}", value, SlotBeginTicks);
        }
    }

    class CircularTimeBuffer
    {
        long[] buffer;
        Queue<TimeSlotResult> results;
        long skippedResults = 0;
        int maxResultQueueLength;
        long ticksPerBucket;
        int buckets;
        int activeDistance;

        public CircularTimeBuffer(long ticksPerBucket, int activeBuckets)
        {
            this.ticksPerBucket = ticksPerBucket;
            // Gap buckets allow us to not lock on increment, just on rotate.
            var gapBuckets = 1;
            this.buckets = activeBuckets + gapBuckets;
            this.activeDistance = buckets - 1 - gapBuckets;

            buffer = new long[buckets];
            results = new Queue<TimeSlotResult>(buckets);
            maxResultQueueLength = buckets * 3;
        }

        long currentHead = 0;
        long pendingHead = 0;
        long initialTail = int.MaxValue;


        object dequeueLock = new object { };

        private void DequeueBuckets(long newHead)
        {
            Utilities.InterlockedMax(ref pendingHead, newHead);
            lock (dequeueLock)
            {
                if (currentHead >= newHead)
                {
                    return; //it's been handled by another thread that was holding the lock.
                }else if (currentHead == 0)
                {
                    currentHead = newHead;
                    return; //First init
                }
                long fromBucketIndex = Math.Max(initialTail, currentHead - buckets + 1);
                long toBucketIndexExclusive = newHead - buckets + 1;
                if (toBucketIndexExclusive > fromBucketIndex)
                {
                    var virtualDequeueCount = (toBucketIndexExclusive - fromBucketIndex);
                    var localDequeueCount = Math.Min(virtualDequeueCount,buckets);
                    Utilities.InterlockedMax(ref skippedResults, 0);
                    Interlocked.Add(ref skippedResults, virtualDequeueCount - localDequeueCount);
                    for (int toDeque = 0; toDeque < localDequeueCount; toDeque++)
                    {
                        var bucketIndex = fromBucketIndex + toDeque;
                        var bufferIndex = bucketIndex % buckets;
                        var result = buffer[bufferIndex];
                        buffer[bufferIndex] = 0;

                        if (results.Count < this.maxResultQueueLength)
                        {
                            results.Enqueue(new TimeSlotResult(result, bucketIndex * ticksPerBucket));
                        }
                        // Interlocked not required, but a memory barrier - maybe. We care about other writes relative to buffer
                        Utilities.InterlockedMax(ref currentHead, bucketIndex + activeDistance);
                    }

                    currentHead = newHead;
                }
            }
        }
        public TimeSlotResult DequeueResult()
        {
            for (var retry = 0; retry < 3; retry++)
            {
                if (results.Count > 0)
                    lock (dequeueLock)
                        if (results.Count > 0)
                            return results.Dequeue();

                if (skippedResults > 0 && Interlocked.Decrement(ref skippedResults) > -1)
                {
                    return TimeSlotResult.Empty;
                }
            }
            return TimeSlotResult.Empty;
        }

        public override string ToString()
        {
            return string.Format("{0} queued, {1} skipped, top: {2}", results.Count, skippedResults, results.Count > 0 ? results.Peek().ToString() : "(empty)");
        }

        public bool Record(long ticks, long count)
        {
            var newIndex = (ticks / ticksPerBucket);

            Utilities.InterlockedMin(ref initialTail, newIndex);

            // Is it new enough we need to clean buckets and shift the active range?
            if (newIndex > currentHead)
            {
                DequeueBuckets(newIndex);
            }

            // Is it too old to record?
            if (newIndex >= currentHead - activeDistance &&
                newIndex >= pendingHead - activeDistance)
            {
                Interlocked.Add(ref buffer[newIndex % buckets], count);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
