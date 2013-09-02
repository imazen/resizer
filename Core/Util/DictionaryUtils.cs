using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Util
{
    class DictionaryUtils
    {
        protected internal static K GetValueOrDefault<K>(IDictionary<string,object> d, string key, K defaultValue) 
        {
            return d.ContainsKey(key) ? (K)d[key] : defaultValue;
        }
    }
}
