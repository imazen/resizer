/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Caching;

namespace ImageResizer.Configuration {
    class CacheSelectionEventArgs :ICacheSelectionEventArgs{
        private System.Web.HttpContext context;

        public System.Web.HttpContext Context {
            get { return context; }
        }
        private IResponseArgs responseArgs;

        public IResponseArgs ResponseArgs {
            get { return responseArgs; }
        }
        private ICache selectedCache;

        public ICache SelectedCache {
            get { return selectedCache; }
            set { selectedCache = value; }
        }

        public CacheSelectionEventArgs(System.Web.HttpContext context, IResponseArgs responseArgs, ICache defaultCache) {
            this.context = context;
            this.responseArgs = responseArgs;
            this.selectedCache = defaultCache;
        }
    }
}
