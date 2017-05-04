using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins
{
    public interface IPluginInfo
    {
        /// <summary>
        /// Provides information that can be reported on the diagnostics page 
        /// and/or collected.
        /// </summary>
        /// <returns></returns>
        IEnumerable<KeyValuePair<string, string>> GetInfoPairs();

    }
}
