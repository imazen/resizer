/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Collections.Specialized;

namespace ImageResizer.Caching {
    public class ResponseHeaders :IResponseHeaders {

        public ResponseHeaders() {
            DefaultHeaders = new NameValueCollection();
            Headers = new NameValueCollection();
            ApplyDuringPreSendRequestHeaders = true;
            CacheControl = HttpCacheability.Private;
            SuppressVaryHeader = true;
            ValidUntilExpires = false;
            LastModified = DateTime.MinValue;
            Expires = DateTime.MinValue;
            ContentType = null;
            ServerCacheDependencies = new List<CacheDependency>();
            this.ApplyToResponse = new ApplyResponseHeadersDelegate(ResponseHeaders.DefaultApplyToResponseMethod);
        }

        public Boolean ApplyDuringPreSendRequestHeaders { get; set; }
        
        /// <summary>
        /// The mime-type of the encoded image. Defaults to null
        /// </summary>
        public string ContentType { get; set; }



        public static void DefaultApplyToResponseMethod(IResponseHeaders headers, HttpContext context) {
            
            //Apply defaults
            foreach (string key in headers.DefaultHeaders)
                context.Response.Headers[key] = headers.DefaultHeaders[key];

            //Set the Content-Type: header
            if (headers.ContentType != null) context.Response.ContentType = headers.ContentType;
            //Sets the Expires: header
            if (headers.Expires != DateTime.MinValue) context.Response.Cache.SetExpires(headers.Expires);
            //Sets the Last-Modifed: header
            //The check against the current time is because  files served from another server may have a modified date in the future, if the clocks are not synchronized.
            //ASP.NET incorrectly blocks an future modified date from being sent, with an ArgumentOutOfRangeException
            DateTime utc = headers.LastModified.ToUniversalTime();

            if (utc != DateTime.MinValue && utc < DateTime.UtcNow) {
                context.Response.Cache.SetLastModified(utc);
            }

            //Valid until expires (I.e, ignore refresh requests)
            context.Response.Cache.SetValidUntilExpires(headers.ValidUntilExpires);

            //Vary by the querystring
            //context.Response.Cache.VaryByParams["*"] = true;


            //Omit the Vary: * 
            context.Response.Cache.SetOmitVaryStar(headers.SuppressVaryHeader);
            //Add dependencies to the server cache
            foreach (CacheDependency d in headers.ServerCacheDependencies)
                context.Response.AddCacheDependency(d);

            //Set Cache-Control: header
            context.Response.Cache.SetCacheability(headers.CacheControl); 
            
            //Apply new headers
            foreach (string key in headers.Headers)
                context.Response.Headers[key] = headers.Headers[key];
        }

        public ApplyResponseHeadersDelegate ApplyToResponse { get; set; }

        public HttpCacheability CacheControl { get; set; }

        public DateTime Expires { get; set; }

        public DateTime LastModified {get;set;}

        public bool ValidUntilExpires {get;set;}

        public bool SuppressVaryHeader { get; set; }

        public NameValueCollection DefaultHeaders { get; set; }

        public NameValueCollection Headers { get; set; }

        public List<CacheDependency> ServerCacheDependencies { get; set; }


    }
}
