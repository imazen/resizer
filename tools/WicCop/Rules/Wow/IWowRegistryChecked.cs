using System;
using System.Collections.Generic;

namespace Microsoft.Test.Tools.WicCop.Rules.Wow
{
    interface IWowRegistryChecked
    {
        IEnumerable<string> GetKeys();
    }
}
