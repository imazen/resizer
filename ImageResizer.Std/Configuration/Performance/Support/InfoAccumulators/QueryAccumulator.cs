using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    class QueryAccumulator
    {
        readonly List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();

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
