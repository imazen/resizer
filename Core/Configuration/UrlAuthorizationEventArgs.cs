using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace ImageResizer.Configuration {
    public class UrlAuthorizationEventArgs : EventArgs, IUrlAuthorizationEventArgs {

        private bool allowAccess = true;

        public bool AllowAccess {
            get { return allowAccess; }
            set { allowAccess = value; }
        }
        public UrlAuthorizationEventArgs(string virtualPath, NameValueCollection queryString, bool allowAccess) {
            this._virtualPath = virtualPath;
            this._queryString = queryString;
            this.allowAccess = allowAccess;
        }


        public NameValueCollection QueryString {
            get { return _queryString; }
        }
        public string VirtualPath {
            get { return _virtualPath; }
        }

        protected string _virtualPath;
        protected NameValueCollection _queryString;


    }
}
