using ImageResizer.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Storage
{
    public class Blob: IVirtualFile, IVirtualFileSourceCacheKey, IVirtualFileWithModifiedDate
    {
        public Blob(BlobProviderBase provider, string virtualPath, NameValueCollection queryString)
        {
            Provider = provider;
            VirtualPath = virtualPath;
            Query = queryString;
        }

        public BlobProviderBase Provider { get; private set; }
        public string VirtualPath
        {
            get;
            protected set;
        }
        public NameValueCollection Query
        {
            get;
            protected set;
        }

        public System.IO.Stream Open()
        {
            return Provider.Open(VirtualPath, Query);
        }

        public string GetCacheKey(bool includeModifiedDate)
        {
            return VirtualPath + (includeModifiedDate ? ("_" + ModifiedDateUTC.Ticks.ToString()) : "");
        }

        public DateTime ModifiedDateUTC
        {
            get {
                if (!Provider.CheckForModifiedFiles) return DateTime.MinValue;
                var date = Provider.GetModifiedDateUtc(VirtualPath,Query);
                if (date == null) return DateTime.MinValue;
                return date.Value;
            }
        }

        public IBlobMetadata FetchMetadata()
        {
            return Provider.FetchMetadataCached(VirtualPath, Query);
        }
    }
}
