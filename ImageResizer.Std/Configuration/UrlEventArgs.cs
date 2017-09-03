// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿
using System;
using System.Collections.Specialized;

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
