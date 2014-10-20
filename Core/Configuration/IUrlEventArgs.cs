/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace ImageResizer.Configuration {
    public interface IUrlEventArgs {
        NameValueCollection QueryString { get; set; }
        /// <summary>
        /// Domain-relative path which also includes the path info portion. I.e, '/app/folder/file.aspx/path/info' could be a valid value. Relative to the domain root, not the site or app root.
        /// </summary>
        string VirtualPath { get; set; }
    }
}
