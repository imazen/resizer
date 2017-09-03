using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.ExtensionMethods
{
    public static class GenericExtensions
    {
        public static string Delimited<T>(this IEnumerable<T> values, string separator) =>
            string.Join(separator, values);

        public static string Delimited(this string[] values, string separator) =>
            string.Join(separator, values);

        public static TResult MapNonNull<T, TResult>(this T v, Func<T, TResult> mapNonNull)
            where T : class where TResult : class => v == null ? null : mapNonNull(v);

        public static TResult? MapNonNull<T, TResult>(this T v, Func<T, TResult?> mapNonNull)
            where T : class where TResult : struct => v == null ? null : mapNonNull(v);

        public static TResult? MapNonNull<T, TResult>(this T? v, Func<T, TResult?> mapNonNull)
            where T : struct where TResult : struct => v == null ? null : mapNonNull(v.Value);

        public static TResult MapNonNull<T, TResult>(this T? v, Func<T, TResult> mapNonNull)
            where T : struct where TResult : class => v == null ? null : mapNonNull(v.Value);
    }
}
