using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Hosting;
using System.Collections.Specialized;

namespace ImageResizer.Plugins {
    public interface IVirtualImageProvider {
        bool FileExists(string virtualPath, NameValueCollection queryString);
        IVirtualFile GetFile(string virtualPath, NameValueCollection queryString);
    }
}
