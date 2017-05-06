using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    class QueryAccumulator
    {
        List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();

        public IInfoAccumulator Object { get; private set; }
        public QueryAccumulator()
        {
            Object = new ProxyAccumulator(false,
                (k, v) => pairs.Add(new KeyValuePair<string, string>(k, v)),
                 (k, v) => pairs.Insert(0, new KeyValuePair<string, string>(k, v)),
                () => pairs);
        }
    }
}
