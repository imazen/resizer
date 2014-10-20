using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.DiskCache
{
    public interface ILockProvider
    {
        bool MayBeLocked(string key);
        bool TryExecute(string key, int timeoutMs, LockCallback success);
    }
}
