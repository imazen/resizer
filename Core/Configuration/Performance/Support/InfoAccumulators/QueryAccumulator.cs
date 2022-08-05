using System.Collections.Generic;

namespace ImageResizer.Configuration.Performance
{
    internal class QueryAccumulator
    {
        private readonly List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();

        public IInfoAccumulator Object { get; }

        public QueryAccumulator()
        {
            Object = new ProxyAccumulator(false,
                (k, v) => pairs.Add(new KeyValuePair<string, string>(k, v)),
                (k, v) => pairs.Insert(0, new KeyValuePair<string, string>(k, v)),
                () => pairs);
        }
    }
}