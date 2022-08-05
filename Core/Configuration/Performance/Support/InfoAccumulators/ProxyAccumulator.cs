using System;
using System.Collections.Generic;

namespace ImageResizer.Configuration.Performance
{
    internal class ProxyAccumulator : IInfoAccumulator
    {
        private readonly Action<string, string> add;
        private readonly Action<string, string> prepend;
        private readonly bool use_prepend = false;
        private readonly Func<IEnumerable<KeyValuePair<string, string>>> fetch;

        public ProxyAccumulator(bool usePrepend, Action<string, string> add, Action<string, string> prepend,
            Func<IEnumerable<KeyValuePair<string, string>>> fetch)
        {
            use_prepend = usePrepend;
            this.add = add;
            this.prepend = prepend;
            this.fetch = fetch;
        }

        public void AddString(string key, string value)
        {
            if (use_prepend)
                prepend(key, value);
            else
                add(key, value);
        }

        public IEnumerable<KeyValuePair<string, string>> GetInfo()
        {
            return fetch();
        }

        public IInfoAccumulator WithPrefix(string prefix)
        {
            return new ProxyAccumulator(use_prepend, (k, v) => add(prefix + k, v), (k, v) => prepend(prefix + k, v),
                fetch);
        }

        public IInfoAccumulator WithPrepend(bool prepending)
        {
            return new ProxyAccumulator(prepending, add, prepend, fetch);
        }
    }
}