using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ImageResizer.Configuration.Performance
{

    class Utilities
    {
        public static string Sha256hex(string input)
        {
            var hash = System.Security.Cryptography.SHA256.Create().ComputeHash(UTF8Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash, 0, 4).Replace("-", "").ToLowerInvariant();
        }

        public static string Sha256Base64(string input)
        {
            var hash = System.Security.Cryptography.SHA256.Create().ComputeHash(UTF8Encoding.UTF8.GetBytes(input));
            return PathUtils.ToBase64U(hash);
        }

        public static string Sha256TruncatedBase64(string input, int bytes)
        {
            var hash = System.Security.Cryptography.SHA256.Create().ComputeHash(UTF8Encoding.UTF8.GetBytes(input));
            return PathUtils.ToBase64U(hash.Take(bytes).ToArray());
        }

        public static void InterlockedMax(ref long location1, long other)
        {
            long copy;
            long max;
            {
                copy = Interlocked.Read(ref location1);
                max = Math.Max(other, copy);
            } while (max > copy && Interlocked.CompareExchange(ref location1, max, copy) != copy) ;
        }
        public static void InterlockedMin(ref long location1, long other)
        {
            long copy;
            long min;
            {
                copy = Interlocked.Read(ref location1);
                min = Math.Min(other, copy);
            } while (min < copy && Interlocked.CompareExchange(ref location1, min, copy) != copy) ;
        }

    }

    internal static class BoolExtensions
    {
        public static string ToShortString(this bool b)
        {
            return b ? "1" : "0";
        }

    }
    internal static class StringExtensions
    {
        /// <summary>
        /// Only lowercases A..Z -> a..z, and only if req.d.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToLowerOrdinal(this string s)
        {
            StringBuilder b = null;
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c >= 'A' && c <= 'Z')
                {
                    if (b == null) b = new StringBuilder(s);
                    b[i] = (char)(c + 0x20);
                }
            }
            return b?.ToString() ?? s;
        }
    }

    internal static class AssemblyExtensions
    {
        public static T GetFirstAttribute<T>(this Assembly a)
        {
            try
            {
                object[] attrs = a.GetCustomAttributes(typeof(T), false);
                if (attrs != null && attrs.Length > 0) return (T)attrs[0];
            }
            catch { }
            return default(T);
        }

        public static string GetShortCommit(this Assembly a)
        {
            return string.Concat(GetFirstAttribute<CommitAttribute>(a)?.Value.Take(7));

        }
        public static string GetInformationalVersion(this Assembly a)
        {
            return GetFirstAttribute<AssemblyInformationalVersionAttribute>(a)?.InformationalVersion;
        }
        public static string GetFileVersion(this Assembly a)
        {
            return GetFirstAttribute<AssemblyFileVersionAttribute>(a)?.Version;
        }
    }
        /// <summary>
        /// Wraps a value factory, and ensures that subsequent values are never smaller than previous values. 
        /// </summary>
        struct OnlyIncreasingValue
    {
        long value;
        public long GetLargerValue(Func<long> candidateValueFactory)
        {
            long initialValue;
            long computedValue;
            do
            {
                var candidateValue = candidateValueFactory();
                initialValue = value;
                computedValue = Math.Max(initialValue, candidateValue);
            } while (initialValue != Interlocked.CompareExchange(ref value, computedValue, initialValue));
            return computedValue;
        }
    }


    ///// <summary>
    ///// What value are X percentage of values under? Returns null if the percentage does not include at least minSamples. Returns MaxValue if there are not enough in sampling range.
    ///// </summary>
    ///// <param name="percentile"></param>
    ///// <returns></returns>
    //public long? PercentileUnder(double percentile, long minSamples)
    //{
    //    lock (this.setLock)
    //    {
    //        long collectCount = (long)(percentile * (double)totalCount);
    //        if (collectCount < minSamples) return null;

    //        long collected = this.underCount;
    //        long upperBound = this.minValue;
    //        int bucketIndex = 0;
    //        while (collected < collectCount)
    //        {
    //            if (bucketIndex >= buckets.Length)
    //            {
    //                return long.MaxValue;
    //            }
    //            collected += buckets[bucketIndex];
    //            upperBound += bucketSize;
    //            bucketIndex++;
    //        }
    //        return upperBound;
    //    }

    //}
}
