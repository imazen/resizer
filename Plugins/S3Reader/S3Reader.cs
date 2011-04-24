/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Hosting;

namespace ImageResizer.Plugins.S3Reader {
    public class S3Reader : IPlugin {



        public IPlugin Install(Configuration.Config c) {
            //S3VirtualPathProvider provider = new S3VirtualPathProvider(
            //HostingEnvironment.RegisterVirtualPathProvider(new S3VirtualPathProvider(
            //            delegate(S3VirtualPathProvider s, S3PathEventArgs ev)
            //            {
            //                ev.AssertBucketMatches("codinghorrorimg");
            //            }
            //, true).AddDiskCachingHook());

            //        /// <summary>
            //    /// Adds a hook to the image resizer (an event handler for RewriteDefaults) that sets useresizingpipeline=true for all requests that would hit this VPP
            //    /// </summary>
            //    /// <returns></returns>
            //    public S3VirtualPathProvider AddDiskCachingHook()
            //    {
            //        S3VirtualPathProvider me = this;
            //        //Force all s3 files to go through the resizing pipeline and get cached. The VirtualPathProvider sends a bitmap to the resizer - very little overhead doing it this way.
            //        fbs.ImageResizer.InterceptModule.RewriteDefaults += delegate(fbs.ImageResizer.InterceptModule s, fbs.ImageResizer.UrlEventArgs ev)
            //        {
            //            if (me.IsPathVirtual(ev.VirtualPath)) ev.QueryString["useresizingpipeline"] = "true";
            //        };
            //        return this;
            //    }

            return this;

        }

        public bool Uninstall(Configuration.Config c) {
            return false;
        }

    }
}
