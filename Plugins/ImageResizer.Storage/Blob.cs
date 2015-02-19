// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using ImageResizer.Plugins;
using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Storage
{
    public class Blob : IVirtualFile, IVirtualFileAsync, IVirtualFileSourceCacheKey, IVirtualFileWithModifiedDate, IVirtualFileWithModifiedDateAsync   
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

        public Stream Open()
        {
            return AsyncUtils.RunSync<Stream>(() => Provider.OpenAsync(VirtualPath, Query));
        }

        public string GetCacheKey(bool includeModifiedDate)
        {
            return VirtualPath + (includeModifiedDate ? ("_" + ModifiedDateUTC.Ticks.ToString()) : "");
        }

        public DateTime ModifiedDateUTC
        {
            get {
                if (!Provider.CheckForModifiedFiles) return DateTime.MinValue;
                return AsyncUtils.RunSync<DateTime>(() => GetModifiedDateUTCAsync());
            }
        }

        public Task<IBlobMetadata> FetchMetadataAsync()
        {
            return Provider.FetchMetadataCachedAsync(VirtualPath, Query);
        }

        public async Task<DateTime> GetModifiedDateUTCAsync()
        {
            if (!Provider.CheckForModifiedFiles) return DateTime.MinValue;
            var date = await Provider.GetModifiedDateUtcAsync(VirtualPath, Query);
            return date != null ? date.Value : DateTime.MinValue;
        }

        public Task<System.IO.Stream> OpenAsync()
        {
            return Provider.OpenAsync(VirtualPath, Query);
        }
    }
}
