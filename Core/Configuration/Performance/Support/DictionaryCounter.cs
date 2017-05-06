using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    class DictionaryCounter<T>
    {
        ConcurrentDictionary<T, Counter> dict;
        Counter count;
        Counter otherCount;

        public int MaxKeyCount { get; private set; }
        public T LimitExceededKey { get; private set; }

        public DictionaryCounter(T updateFailedKey) : this(int.MaxValue, updateFailedKey, EqualityComparer<T>.Default)
        { }

        public DictionaryCounter(int maxKeyCount, T limitExceededKey) : this(maxKeyCount, limitExceededKey, EqualityComparer<T>.Default)
        { }
        public DictionaryCounter(int maxKeyCount, T limitExceededKey, IEqualityComparer<T> comparer) :
            this(maxKeyCount, limitExceededKey, Enumerable.Empty<KeyValuePair<T, long>>(), comparer)
        { }
        
        public DictionaryCounter(int maxKeyCount, T limitExceededKey, IEnumerable<KeyValuePair<T, long>> initial, IEqualityComparer<T> comparer)
        {
            MaxKeyCount = maxKeyCount;
            LimitExceededKey = limitExceededKey;
            otherCount = new Counter(0);
            var initialPairs = initial
                .Select(pair => new KeyValuePair<T, Counter>(pair.Key, new Counter(pair.Value)))
                .Take(maxKeyCount - 1)
                .Concat(new[] { new KeyValuePair<T, Counter>(LimitExceededKey, otherCount) });
          
            dict = new ConcurrentDictionary<T, Counter>(initialPairs, comparer);
            count = new Counter(dict.Count);
        }

        public bool TryRead(T key, out long v)
        {
            Counter c;
            if (dict.TryGetValue(key, out c))
            {
                v = c.Value;
                return true;
            }
            else
            {
                v = 0;
                return false;
            }
        }

        public bool Contains(T key)
        {
            return dict.ContainsKey(key);
        }

        private Counter GetOrAddInternal(T key, long initialValue, bool applyLimitSwap)
        {
            for (var retryCount = 0; retryCount < 10; retryCount++) { 
                Counter result;
                if (dict.TryGetValue(key, out result))
                {
                    return result;
                } else
                {
                    if (!applyLimitSwap)
                    {
                        count.Increment();
                        var newValue = new Counter(initialValue);
                        result = dict.GetOrAdd(key, newValue);
                        if (result != newValue)
                        {
                            count.Decrement();
                        }
                        return result;
                    }
                    else
                    {
                        var existingSize = count.Value;
                        if (existingSize < MaxKeyCount)
                        {
                            if (count.IncrementIfMatches(existingSize))
                            {
                                var newValue = new Counter(initialValue);
                                result = dict.GetOrAdd(key, newValue);
                                if (result != newValue)
                                {
                                    count.Decrement();
                                }
                                return result;
                            }else
                            {
                                continue; //Let's retry
                            }
                        }else
                        {
                            return otherCount;
                        }
                    }
                }
            }

            // TODO: remove
            throw new Exception();
            // When 
            return otherCount;
        }

       

        /// <summary>
        /// Creates the entry if it doesn't already exist
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long Increment(T key)
        {
            return GetOrAddInternal(key, 0, MaxKeyCount != int.MaxValue).Increment();
        }


        public IEnumerable<KeyValuePair<T,long>> GetCounts()
        {
            return dict.Select(pair => new KeyValuePair<T, long>(pair.Key, pair.Value.Value));
        }
    }
}
