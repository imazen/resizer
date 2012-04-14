using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins {
    public interface IUrlPostFilter {
        double PostFilterOrderHint { get; }

        void PostFilterUrl(ref string uri, ref UrlOptions urlOptions);
    }
}
