using System.Threading;

namespace ImageResizer.Configuration.Performance
{
    internal class Counter
    {
        public Counter(long initialValue)
        {
            v = initialValue;
        }

        private long v = 0;

        public long Increment()
        {
            return Interlocked.Increment(ref v);
        }

        public long Decrement()
        {
            return Interlocked.Decrement(ref v);
        }

        public bool IncrementIfMatches(long comparisonValue)
        {
            return Interlocked.CompareExchange(ref v, comparisonValue + 1, comparisonValue) == comparisonValue;
        }

        public long Value => Interlocked.Read(ref v);
    }
}