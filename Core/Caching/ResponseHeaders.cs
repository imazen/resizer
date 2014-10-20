/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Collections.Specialized;
using System.Web.Hosting;

namespace ImageResizer.Caching {
    public class ResponseHeaders :IResponseHeaders {

        public ResponseHeaders() {
            DefaultHeaders = new NameValueCollection();
            Headers = new NameValueCollection();
            ApplyDuringPreSendRequestHeaders = true;
            CacheControl = HttpCacheability.ServerAndPrivate;
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
            //The check against the current time is because  files served from another server may have a modified date in the future, if the clocks are not synchronized.
            //ASP.NET incorrectly blocks an future modified date from being sent, with an ArgumentOutOfRangeException
            DateTime? utc = headers.LastModified.ToUniversalTime();
            if (headers.LastModified == DateTime.MinValue || utc == DateTime.MinValue || utc >= DateTime.UtcNow) utc = null;

            //We can only update headers in Integrated mode. Otherwise we have to clear them to be able to change them.
            if (!HttpRuntime.UsingIntegratedPipeline) context.Response.ClearHeaders();
            else foreach (string key in headers.DefaultHeaders)
                    context.Response.Headers[key] = headers.DefaultHeaders[key];

            //Set the Content-Type: header
            if (headers.ContentType != null) context.Response.ContentType = headers.ContentType;
            //Sets the Expires: header
            if (headers.Expires != DateTime.MinValue) context.Response.Cache.SetExpires(headers.Expires);
            //Sets the Last-Modifed: header
            if (utc != null) context.Response.Cache.SetLastModified(utc.Value);
            //Valid until expires (I.e, ignore refresh requests)
            context.Response.Cache.SetValidUntilExpires(headers.ValidUntilExpires);
            //Omit the Vary: * 
            context.Response.Cache.SetOmitVaryStar(headers.SuppressVaryHeader);
            //Add dependencies to the server cache
            foreach (CacheDependency d in headers.ServerCacheDependencies)
                context.Response.AddCacheDependency(new CacheDependency[]{d});

            //Set Cache-Control: header
            context.Response.Cache.SetCacheability(headers.CacheControl);

            //Set ETag in a asp.net friendly manner.
            if (headers.Headers["ETag"] != null) {
                context.Response.Cache.SetETag(headers.Headers["ETag"]);
                headers.Headers.Remove("ETag");
            }

            //Apply new headers
            if (HttpRuntime.UsingIntegratedPipeline) {
                foreach (string key in headers.Headers)
                    context.Response.Headers[key] = headers.Headers[key];
            } else {
                //Merge defaults and final headers
                NameValueCollection merged = new NameValueCollection(headers.DefaultHeaders);
                foreach (string key in headers.Headers) merged[key] = headers.Headers[key];
                //Append each, remember ASP.NET Classic will reject those already present.
                foreach (string key in merged)
                    context.Response.AppendHeader(key, merged[key]);

            }

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
