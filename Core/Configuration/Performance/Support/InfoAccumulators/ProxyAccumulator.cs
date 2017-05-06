using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    class ProxyAccumulator : IInfoAccumulator
    {
        Action<string, string> add;
        Action<string, string> prepend;
        bool use_prepend = false;
        Func<IEnumerable<KeyValuePair<string, string>>> fetch;
        public ProxyAccumulator(bool use_prepend, Action<string, string> add, Action<string, string> prepend, Func<IEnumerable<KeyValuePair<string, string>>> fetch)
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
            }
            else
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
