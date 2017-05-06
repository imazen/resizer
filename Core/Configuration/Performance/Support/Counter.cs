using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    class Counter
    {
        public Counter(long initialValue)
        {
            v = initialValue;
        }
        long v = 0;
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
        public long Value { get { return Interlocked.Read(ref v); } }
    }
   
}
