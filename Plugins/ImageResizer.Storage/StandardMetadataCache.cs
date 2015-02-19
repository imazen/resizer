// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ImageResizer.Storage
{
    public class StandardMetadataCache: IMetadataCache
    {
        public StandardMetadataCache()
        {

        }

        private TimeSpan _metadataAbsoluteExpiration = TimeSpan.MaxValue;
        /// <summary>
        /// Existence and modified date metadata about files is cached for, at longest, this amount of time after it is first stored.
        /// </summary>
        public TimeSpan MetadataAbsoluteExpiration
        {
            get
            {
                return _metadataAbsoluteExpiration;//1 hr
            }
            set
            {
                if (!(value == TimeSpan.MaxValue || MetadataSlidingExpiration == TimeSpan.Zero)) 
                    throw new ArgumentException("MetadataAbsoluteExpiration must be DateTime.MaxValue or MetadataSlidingExpiration must be timeSpan.Zero.");
                _metadataAbsoluteExpiration = value;
            }
        }
        private TimeSpan _metadataSlidingExpiration = new TimeSpan(0, 1, 0, 0); //one hour
        /// <summary>
        /// Existence and modified date metadata about files is cached for this long after it is last accessed.
        /// </summary>
        public TimeSpan MetadataSlidingExpiration
        {
            get
            {
                return _metadataSlidingExpiration;//1 hr
            }
            set
            {
                if (!(MetadataAbsoluteExpiration == TimeSpan.MaxValue || value == TimeSpan.Zero)) throw new ArgumentException("MetadataAbsoluteExpiration must be DateTime.MaxValue or MetadataSlidingExpiration must be timeSpan.Zero.");
                _metadataSlidingExpiration = value;
            }

        }

        public object Get(string key)
        {
            return HttpRuntime.Cache.Get(key);
        }

        public void Put(string key, object data)
        {
            HttpRuntime.Cache.Insert(key, data, null, MetadataAbsoluteExpiration == TimeSpan.MaxValue ? DateTime.MaxValue : DateTime.UtcNow.Add(MetadataAbsoluteExpiration), MetadataSlidingExpiration);
        }
    }
}
