using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.SourceMemCache {
    internal class ConstrainedCache<K,V> {

        public delegate long SizeCalculationDelegate(K key, V value);
        public ConstrainedCache(IEqualityComparer<K> keyComparer, SizeCalculationDelegate calculator, long maxBytes, TimeSpan usageWindow, TimeSpan minCleanupInterval) {
            EventCountingStrategy s = new EventCountingStrategy();
            s.MaxBytesUsed = maxBytes;
            s.MinimumCleanupInterval = minCleanupInterval;
            s.CounterGranularity = 16;
            usage = new EventCountingDictionary<K>(keyComparer, usageWindow, s);
            usage.CounterRemoved += new EventCountingDictionary<K>.EventKeyRemoved(usage_CounterRemoved);

            data = new Dictionary<K, V>(keyComparer);

            this.calculator = calculator;
        }

        SizeCalculationDelegate calculator;
        private EventCountingDictionary<K> usage;

        private Dictionary<K,V> data;

        /// <summary>
        /// The estimated ram usage for the entire cache. Relies upon the accuracy of the calculator delegate
        /// </summary>
        public long ReportedBytesUsed { get { return usage.ReportedBytesUsed; } }

        private object lockSync = new object();

        public V Get(K key) {
            lock (lockSync) {
                V val;
                bool found = data.TryGetValue(key, out val);
                if (found) usage.Increment(key, 0);
                return found ? val : default(V);
            }
        }

        public void Set(K key, V val) {
            lock (lockSync) {
                data[key] = val;
                usage.Increment(key, calculator(key, val) + 32);
            }
        }

        public void PingCleanup() { usage.PingCleanup(); }

        void usage_CounterRemoved(EventCountingDictionary<K> sender, K key, int value) {
            //May be exected inside usage.lockSync AND lockSync
            lock (lockSync) {
                data.Remove(key);
            }
        }

    }
}
