using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace ImageResizer.Plugins {
    public interface IUrlPreFilter {
        double PreFilterOrderHint { get; }
        void PreFilterUrl(ref string path, ref NameValueCollection query, ref UrlOptions urlOptions);
    }
}
