// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Specialized;

namespace ImageResizer.Configuration
{
    public class UrlAuthorizationEventArgs : EventArgs, IUrlAuthorizationEventArgs
    {
        private bool allowAccess = true;

        public bool AllowAccess
        {
            get => allowAccess;
            set => allowAccess = value;
        }

        public UrlAuthorizationEventArgs(string virtualPath, NameValueCollection queryString, bool allowAccess)
        {
            _virtualPath = virtualPath;
            _queryString = queryString;
            this.allowAccess = allowAccess;
        }


        public NameValueCollection QueryString => _queryString;

        public string VirtualPath => _virtualPath;

        protected string _virtualPath;
        protected NameValueCollection _queryString;
    }
}