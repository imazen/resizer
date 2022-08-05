using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ImageResizer.Configuration.Performance
{
    internal class CircularTimeBuffer
    {
        private readonly long[] buffer;
        private readonly Queue<TimeSlotResult> results;
        private long skippedResults = 0;
        private readonly int maxResultQueueLength;
        private readonly long ticksPerBucket;
        private readonly int buckets;
        private readonly int activeDistance;

        public CircularTimeBuffer(long ticksPerBucket, int activeBuckets)
        {
            this.ticksPerBucket = ticksPerBucket;
            // Gap buckets allow us to not lock on increment, just on rotate.
            var gapBuckets = 1;
            buckets = activeBuckets + gapBuckets;
            activeDistance = buckets - 1 - gapBuckets;

            buffer = new long[buckets];
            results = new Queue<TimeSlotResult>(buckets);
            maxResultQueueLength = buckets * 3;
        }

        private long currentHead = 0;
        private long pendingHead = 0;
        private long initialTail = int.MaxValue;


        private readonly object dequeueLock = new object();

        private void DequeueBuckets(long newHead)
        {
            Utilities.InterlockedMax(ref pendingHead, newHead);
            lock (dequeueLock)
            {
                if (currentHead >= newHead) return; //it's been handled by another thread that was holding the lock.
                if (currentHead == 0)
                {
                    currentHead = newHead;
                    return; //First init
                }

                var fromBucketIndex = Math.Max(initialTail, currentHead - buckets + 1);
                var toBucketIndexExclusive = newHead - buckets + 1;
                if (toBucketIndexExclusive > fromBucketIndex)
                {
                    var virtualDequeueCount = toBucketIndexExclusive - fromBucketIndex;
                    var localDequeueCount = Math.Min(virtualDequeueCount, buckets);
                    Utilities.InterlockedMax(ref skippedResults, 0);
                    if (virtualDequeueCount > localDequeueCount)
                        Interlocked.Add(ref skippedResults, virtualDequeueCount - localDequeueCount);
                    for (var toDeque = 0; toDeque < localDequeueCount; toDeque++)
                    {
                        var bucketIndex = fromBucketIndex + toDeque;
                        var bufferIndex = bucketIndex % buckets;
                        var result = buffer[bufferIndex];
                        buffer[bufferIndex] = 0;

                        if (results.Count < maxResultQueueLength)
                            results.Enqueue(new TimeSlotResult(result, bucketIndex * ticksPerBucket));
                        // Interlocked not required, but a memory barrier - maybe. We care about other writes relative to buffer
                        Utilities.InterlockedMax(ref currentHead, bucketIndex + activeDistance);
                    }

                    currentHead = newHead;
                }
            }
        }

        public IEnumerable<TimeSlotResult> DequeueResults()
        {
            // Don't allocate unless there are actually results
            var toSkip = Utilities.InterlockedMin(ref skippedResults, 0);
            var dequeued = Enumerable.Empty<TimeSlotResult>();

            if (results.Count > 0)
                lock (dequeueLock)
                {
                    if (results.Count > 0)
                    {
                        dequeued = results.ToArray();
                        results.Clear();
                    }
                }

            if (toSkip > 0) dequeued = dequeued.Concat(Enumerable.Repeat(TimeSlotResult.ResultZero, (int)toSkip));
            return dequeued;
        }

        public IEnumerable<long> DequeueValues()
        {
            var enumerableResults = DequeueResults();
            // ReSharper disable once PossibleUnintendedReferenceComparison
            return enumerableResults == Enumerable.Empty<TimeSlotResult>()
                ? Enumerable.Empty<long>()
                : enumerableResults.Select(r => r.Value);
        }

        public override string ToString()
        {
            return
                $"{results.Count} queued, {skippedResults} skipped, top: {(results.Count > 0 ? results.Peek().ToString() : "(empty)")}";
        }

        public bool Record(long ticks, long count)
        {
            if (ticks < 0) return false;

            var newIndex = ticks / ticksPerBucket;

            Utilities.InterlockedMin(ref initialTail, newIndex);

            // Is it new enough we need to clean buckets and shift the active range?
            if (newIndex > currentHead) DequeueBuckets(newIndex);

            // Is it too old to record?
            if (newIndex < currentHead - activeDistance || newIndex < pendingHead - activeDistance) return false;
            Interlocked.Add(ref buffer[newIndex % buckets], count);
            return true;
        }
    }
}