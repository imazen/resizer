/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Caching;
using System.Web;

namespace ImageResizer.Configuration {
    public interface ICacheSelectionEventArgs {
        HttpContext Context {get;}
        IResponseArgs ResponseArgs { get; }
        ICache SelectedCache { get; set; }
    }
}
