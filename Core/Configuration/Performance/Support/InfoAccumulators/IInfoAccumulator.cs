using System.Collections.Generic;

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