using System;
using System.Collections.Generic;

namespace Bench
{
    public static class ExtensionMethods
    {
        public static IEnumerable<Tuple<T, K>> Combine<T, K>(this IEnumerable<T> seq1, IEnumerable<K> seq2)
        {
            foreach (var a in seq1)
            foreach (var b in seq2)
                yield return new Tuple<T, K>(a, b);
        }

        public static IEnumerable<Tuple<T, U, V, W>> FlattenTuples<T, U, V, W>(
            this IEnumerable<Tuple<Tuple<T, U>, Tuple<V, W>>> seq)
        {
            foreach (var a in seq)
                yield return new Tuple<T, U, V, W>(a.Item1.Item1, a.Item1.Item2, a.Item2.Item1, a.Item2.Item2);
        }
    }
}