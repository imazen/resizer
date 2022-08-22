using System;
using System.Collections.Concurrent;

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     For when plugins need communal static storage
    /// </summary>
    public class CommonStaticStorage
    {
        private static ConcurrentDictionary<string, object> dict = new ConcurrentDictionary<string, object>();

        /// <summary>
        ///     Returns the actual value (may not be your factory's result, even if it ran).
        /// </summary>
        /// <param name="key"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static object GetOrAdd(string key, Func<string, object> factory)
        {
            return dict.GetOrAdd(key, factory);
        }

        /// <summary>
        ///     Tries to get the value. Returns false if no value exists.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetValue(string key, out object value)
        {
            return dict.TryGetValue(key, out value);
        }

        /// <summary>
        ///     Updates the value (may not be your factory's result, even if it is run).
        /// </summary>
        /// <param name="key"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static object AddOrUpdate(string key, Func<string, object> factory)
        {
            return dict.AddOrUpdate(key, factory, (s, o) => factory(s));
        }
    }
}