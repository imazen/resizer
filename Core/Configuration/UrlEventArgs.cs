/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Web;
using ImageResizer.Caching;

namespace ImageResizer.Configuration {
    public class UrlEventArgs : EventArgs, IUrlEventArgs {
        protected string _virtualPath;
        protected NameValueCollection _queryString;

        public UrlEventArgs(string virtualPath, NameValueCollection queryString) {
            this._virtualPath = virtualPath;
            this._queryString = queryString;
        }

        public NameValueCollection QueryString {
            get { return _queryString; }
            set { _queryString = value; }
        }
        public string VirtualPath {
            get { return _virtualPath; }
            set { _virtualPath = value; }
        }
    }
}
