using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Hosting;
using Imazen.Common.Issues;

namespace ImageResizer.Plugins
{
    
    /// <summary>
    ///     Not for you. Don't use this. It creates a separate file for every key. Wraps a singleton
    /// </summary>
    [Obsolete("Use internal Imazen.Common.Persistence.PersistentGlobalStringCache instead. This class is not thread-safe and will be removed in a future version.")]
    public class PersistentGlobalStringCache : IPersistentStringCache, IIssueProvider
    {
        private static Imazen.Common.Persistence.PersistentGlobalStringCache inner;
        public PersistentGlobalStringCache()
        {
            if (inner == null)
            {

                var appPath = HostingEnvironment.ApplicationPhysicalPath;
                var candidates = (appPath != null
                    ? new[]
                    {
                        Path.Combine(appPath, "imagecache"),
                        Path.Combine(appPath, "App_Data"), Path.GetTempPath()
                    }
                    : new[] { Path.GetTempPath() }).ToArray();
                inner = new Imazen.Common.Persistence.PersistentGlobalStringCache("resizer_key_", candidates);
            }
        }

        public string Get(string key)
        {
            return inner.Get(key);
        }

        public StringCachePutResult TryPut(string key, string value)
        {
            switch (inner.TryPut(key, value))
            {
                case Imazen.Common.Persistence.StringCachePutResult.Duplicate:
                    return StringCachePutResult.Duplicate;
                case Imazen.Common.Persistence.StringCachePutResult.WriteComplete:
                    return StringCachePutResult.WriteComplete;
                case Imazen.Common.Persistence.StringCachePutResult.WriteFailed:
                    return StringCachePutResult.WriteFailed;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IEnumerable<IIssue> GetIssues()
        {
            return inner.GetIssues();
        }

        public DateTime? GetWriteTimeUtc(string key)
        {
            return inner.GetWriteTimeUtc(key);
        }
    }
}