using System;
using System.Collections.Generic;

namespace ImageResizer.ExtensionMethods
{
    public static class GenericExtensions
    {
        public static string Delimited<T>(this IEnumerable<T> values, string separator)
        {
            return string.Join(separator, values);
        }

        public static string Delimited(this string[] values, string separator)
        {
            return string.Join(separator, values);
        }

        public static TResult MapNonNull<T, TResult>(this T v, Func<T, TResult> mapNonNull)
            where T : class where TResult : class
        {
            return v == null ? null : mapNonNull(v);
        }

        public static TResult? MapNonNull<T, TResult>(this T v, Func<T, TResult?> mapNonNull)
            where T : class where TResult : struct
        {
            return v == null ? null : mapNonNull(v);
        }

        public static TResult? MapNonNull<T, TResult>(this T? v, Func<T, TResult?> mapNonNull)
            where T : struct where TResult : struct
        {
            return v == null ? null : mapNonNull(v.Value);
        }

        public static TResult MapNonNull<T, TResult>(this T? v, Func<T, TResult> mapNonNull)
            where T : struct where TResult : class
        {
            return v == null ? null : mapNonNull(v.Value);
        }
    }
}