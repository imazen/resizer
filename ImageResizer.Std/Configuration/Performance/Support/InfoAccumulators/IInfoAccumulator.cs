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
}
