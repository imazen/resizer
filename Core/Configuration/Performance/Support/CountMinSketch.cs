using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ImageResizer.Configuration.Performance
{
    interface IHash
    {
        uint ComputeHash(uint value);
        IHash GetNext();
    }

    struct AddMulModHash: IHash
    {
        static ulong prime = (ulong)(Math.Pow(2, 32) - 5.0);//(ulong)(Math.Pow(2,33) - 355.0);

        /// <summary>
        /// Seed the random number generator with a good prime, or you'll get poor distribution
        /// </summary>
        /// <param name="r"></param>
        public AddMulModHash(Random r)
        {
            this.r = r;
            this.a = (ulong)(r.NextDouble() * (prime - 2)) + 1;
            this.b = (ulong)(r.NextDouble() * (prime - 2)) + 1;
        }
        ulong a;
        ulong b;
        Random r;
        public uint ComputeHash(uint value)
        {
            return (uint)((a * value + b) % prime);
        }
        
        public IHash GetNext()
        {
            return new AddMulModHash(r);
        }

        public static AddMulModHash DeterministicDefault()
        {
            return new AddMulModHash(new Random(1499840347));
        }
    }

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
            return GetPercentiles(new[] { percentile }, clamp)[0];
        }
        public long[] GetPercentiles(IEnumerable<float> percentiles, SegmentClamping clamp)
        {
            var distinctValues = clamp.PossibleValues()
                                .SelectMany(v => Enumerable.Repeat(v, (int)Estimate((uint)v)))
                                .OrderBy(n => n).ToArray();
            if (distinctValues.Length == 0)
            {
                return Enumerable.Repeat(0L, percentiles.Count()).ToArray();
            }

            return percentiles.Select(percentile =>
            {

                float index = Math.Max(0, percentile * distinctValues.Length + 0.5f);

                return (distinctValues[(int)Math.Max(0,Math.Ceiling(index - 1.5))] +
                        distinctValues[(int)Math.Min(Math.Ceiling(index - 0.5), distinctValues.Length - 1)]) / 2;
               

                //if (Math.Round(index) == index && (int)index != distinctValues.Length - 1)
                //{
                //    return (distinctValues[(int)index] + distinctValues[(int)index + 1]) / 2;
                //}
                //else
                //{
                //    return distinctValues[(int)Math.Ceiling(index)];
                //}
            }).ToArray();

            //var total = clamp.PossibleValues().Sum(v => Estimate((uint)v));
            //var threshold = Math.Min(total, (long)Math.Round(percentile * total));
            //long cumulative = 0;
            //foreach (long value in clamp.PossibleValues())
            //{
            //    long prev = cumulative;
            //    cumulative += Estimate((uint)value);
            //    if (cumulative >= threshold)
            //    {
            //        return prev;
            //    }
            //}
            //// Error!
            //Debug.Assert(false);
            //return total;
        }
    }

    //public class FixedTable
    //{
    //    SegmentClamping clamp;
    //    long count;
    //    long[] table;
    //    public FixedTable(SegmentClamping clamp)
    //    {
    //        this.clamp = clamp;
    //        count = clamp.PossibleValues().Count();
    //        table = new long[count];
    //    }
    //    public void Add(uint value, int count)
    //    {

    //        Interlocked.Add(ref table[])
    //    }
    //}
}
