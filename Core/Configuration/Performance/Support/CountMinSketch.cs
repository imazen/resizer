using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ImageResizer.Configuration.Performance
{
    class CountMinSketch<T> where T: struct, IHash
    {
        int[,] table;
        uint bucketCount;
        uint hashAlgCount;
        T[] hashes;

        public CountMinSketch(uint bucketCount, uint hashAlgCount, T hasher)
        {
            this.bucketCount = bucketCount;
            this.hashAlgCount = hashAlgCount;

            var nextHash = hasher;
            hashes = new T[hashAlgCount];
            for (var i = 0; i < hashAlgCount; i++)
            {
                hashes[i] = nextHash;
                nextHash = (T)hasher.GetNext();
            }

            table = new int[hashAlgCount, bucketCount];
        }
        
        public void InterlockedAdd(uint value, int count)
        {
            //var indicies = hashes.Select(h => h.ComputeHash(value) % bucketCount).ToArray();

            for (var i = 0; i < hashAlgCount; i++)
            {
                var ix = hashes[i].ComputeHash(value) % bucketCount;
                Interlocked.Add(ref table[i, ix], count);
            }
        }
        public void Add(uint value, int count)
        {
            //var indicies = hashes.Select(h => h.ComputeHash(value) % bucketCount).ToArray();

            for (var i = 0; i < hashAlgCount; i++)
            {
                var ix = hashes[i].ComputeHash(value) % bucketCount;
                table[i, ix] += count;
            }
        }

        public long Estimate(uint value)
        {
            // var hashValues = hashes.Select((hash, hashIndex) => hash.ComputeHash(value)).ToArray();
            // var indicies = hashes.Select((hash, hashIndex) => hash.ComputeHash(value) % bucketCount).ToArray();
            // var values = hashes.Select((hash, hashIndex) => table[hashIndex, hash.ComputeHash(value) % bucketCount]).ToArray();

            int result = int.MaxValue;
            for (var i = 0; i < hashAlgCount; i++)
            {
                var cell = table[i, hashes[i].ComputeHash(value) % bucketCount];
                result = cell < result ? cell : result;
            }
            return result;
            //return hashes.Select((hash, hashIndex) => table[hashIndex, hash.ComputeHash(value) % bucketCount]).Min();
        }

        public string DebugTable(bool skipEmptyRows = true)
        {
            var sb = new StringBuilder();
            for (var b = 0; b < bucketCount; b++)
            {
                var row = Enumerable.Range(0, (int)hashAlgCount).Select(ix => table[ix, b]);
                if (!skipEmptyRows || row.Any(v => v > 0))
                {
                    //Print the bucket index
                    sb.AppendFormat("[{0,-5}]  ", b);

                    foreach (var cell in row)
                    {
                        sb.AppendFormat("{0,10} ", cell);
                    }
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        public void Clear()
        {
            Array.Clear(table, 0, table.Length);
        }

        /// <summary>
        /// Gets a sorted list of all values that have been recorded 
        /// (and, of course, some that haven't, due to hash collisions)
        /// values recorded multiple times will appear multiple times;
        /// </summary>
        /// <param name="clamp"></param>
        /// <returns></returns>
        public long[] GetAllValues(SegmentClamping clamp)
        {
            return clamp.PossibleValues()
                                 .SelectMany(v => Enumerable.Repeat(v, (int)Estimate((uint)v)))
                                 .OrderBy(n => n).ToArray();
        }

        public long GetPercentile(float percentile, SegmentClamping clamp)
        {
            return GetAllValues(clamp).GetPercentile(percentile);
        }
        public long[] GetPercentiles(IEnumerable<float> percentiles, SegmentClamping clamp)
        {
            var set = GetAllValues(clamp);
            return percentiles.Select(p => set.GetPercentile(p)).ToArray();
        }
    }

}
