/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;

///<summary>
///All plugins in this namespace are licensed under the Freedom license
///</summary>
namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Provides default client-caching behavior. Sends Last-Modified header if present, and Expires header if &ltclientcache minutes="value" /> is configured.
    /// Also defaults Cache-control to Public for anonymous requests (and private for authenticated requests)
    /// </summary>
    public class ClientCache:IPlugin{

        Config c;
        public IPlugin Install(Configuration.Config c) {
            this.c = c;
            c.Plugins.add_plugin(this);
            c.Pipeline.PreHandleImage += Pipeline_PreHandleImage; 
            return this;
        }

        /*  http://developer.yahoo.com/performance/rules.html
            http://24x7aspnet.blogspot.com/2009/06/using-cache-methods-httpcacheability-in.html

            Redirects should have caching headers.
            Expires: is good
            Remove ETags, bad server implementation
         */

        void Pipeline_PreHandleImage(System.Web.IHttpModule sender, System.Web.HttpContext context, Caching.IResponseArgs e) {
            int mins = c.get("clientcache.minutes", -1);
            //Set the expires value if present
            if (mins > 0)
                e.ResponseHeaders.Expires = DateTime.UtcNow.AddMinutes(mins);

            //Send the last-modified date if present
            DateTime lastModified = e.GetModifiedDateUTC();
            if (lastModified != DateTime.MinValue) e.ResponseHeaders.LastModified = lastModified;

            //Authenticated requests only allow caching on the client. 
            //Anonymous requests get caching on the server, proxy and client
            if (context.Request.IsAuthenticated)
                e.ResponseHeaders.CacheControl = System.Web.HttpCacheability.Private;
            else
                e.ResponseHeaders.CacheControl = System.Web.HttpCacheability.Public;

        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PreHandleImage -= Pipeline_PreHandleImage;
            return false;
        }
    }
}
