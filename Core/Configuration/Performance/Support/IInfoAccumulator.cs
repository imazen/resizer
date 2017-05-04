using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    public interface IInfoAccumulator
    {
        void AddString(string key, string value);
        IInfoAccumulator WithPrefix(string prefix);
        IInfoAccumulator WithPrepend(bool prepend);
        IEnumerable<KeyValuePair<string, string>> GetInfo();
    }
    public static class InfoAccumuatorExtensions {

        public static void Add(this IInfoAccumulator a, string key, Guid value)
        {
            a.AddString(key, PathUtils.ToBase64U(value.ToByteArray()));
        }

        public static void Add(this IInfoAccumulator a, IEnumerable<KeyValuePair<string, string>> items)
        {
            foreach (var pair in items)
            {
                a.AddString(pair.Key, pair.Value);
            }
        }

        public static void Add(this IInfoAccumulator a, string key, bool? value)
        {
            a.AddString(key, value?.ToShortString());
        }

        public static void Add(this IInfoAccumulator a, string key, long? value)
        {
            a.AddString(key, value?.ToString());
        }

        public static void Add(this IInfoAccumulator a, string key, string value)
        {
            a.AddString(key, value);
        }
        public static string ToQueryString(this IInfoAccumulator a, int characterLimit)
        {
            var pairs = a.GetInfo().Where(pair => pair.Value != null && pair.Key != null)
                     .Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value));
            var sb = new StringBuilder(1000);
            sb.Append("?");
            foreach(var s in pairs)
            {
                if (sb.Length + s.Length + 1 > characterLimit)
                {
                    return sb.ToString();
                }else
                {
                    if (sb[sb.Length -1] != '?') sb.Append("&");
                    sb.Append(s);
                }
            }
            return sb.ToString();
        }
       

    }

    class QueryAccumulator
    {
        List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();

        public IInfoAccumulator Object { get; private set; }
        public QueryAccumulator()
        {
            Object = new ProxyAccumulator(false, 
                (k, v) => pairs.Add(new KeyValuePair<string, string>(k, v)),
                 (k, v) => pairs.Insert(0,new KeyValuePair<string, string>(k, v)),
                () => pairs);
        }
    }


    class ProxyAccumulator : IInfoAccumulator
    {
        Action<string, string> add;
        Action<string, string> prepend;
        bool use_prepend = false;
        Func<IEnumerable<KeyValuePair<string, string>>> fetch;
        public ProxyAccumulator(bool use_prepend, Action<string, string> add, Action<string, string> prepend, Func<IEnumerable<KeyValuePair<string,string>>> fetch)
        {
            this.use_prepend = use_prepend;
            this.add = add;
            this.prepend = prepend;
            this.fetch = fetch;
        }
        public void AddString(string key, string value)
        {
            if (use_prepend)
            {
                add(key, value);
            }else
            {
                prepend(key, value);
            }
        }

        public IEnumerable<KeyValuePair<string, string>> GetInfo()
        {
            return fetch();
        }

        public IInfoAccumulator WithPrefix(string prefix)
        {
            return new ProxyAccumulator(use_prepend, (k, v) => add(prefix + k, v), (k, v) => prepend(prefix + k, v), fetch);
        }

        public IInfoAccumulator WithPrepend(bool prepending)
        {
            return new ProxyAccumulator(prepending, add, prepend, fetch);
        }
    }

}
