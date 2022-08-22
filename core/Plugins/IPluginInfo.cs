using System.Collections.Generic;

namespace ImageResizer.Plugins
{
    public interface IPluginInfo
    {
        /// <summary>
        ///     Provides information that can be reported on the diagnostics page
        ///     and/or collected.
        /// </summary>
        /// <returns></returns>
        IEnumerable<KeyValuePair<string, string>> GetInfoPairs();
    }
}