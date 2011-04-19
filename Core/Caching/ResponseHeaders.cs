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
            this.ApplyToResponse = new ApplyResponseHeadersDelegate(ResponseHeaders.DefaultApplyToResponseMethod);
        }

        protected Boolean applyDuringPreSendRequestHeaders = true;
       
        public Boolean ApplyDuringPreSendRequestHeaders {
            get { return applyDuringPreSendRequestHeaders; }
            set { applyDuringPreSendRequestHeaders = value; }
        }
        protected string contentType = null;
        /// <summary>
        /// The mime-type of the encoded image. Defaults to null
        /// </summary>
        public string ContentType {
            get { return contentType; }
            set { contentType = value; }
        }



        public static void DefaultApplyToResponseMethod(IResponseHeaders headers, HttpContext context) {
            
            //Apply defaults
            foreach (string key in headers.DefaultHeaders)
                context.Response.Headers[key] = headers.DefaultHeaders[key];

            //Set the Content-Type: header
            if (headers.ContentType != null) context.Response.ContentType = headers.ContentType;
            //Sets the Expires: header
            if (headers.Expires != DateTime.MinValue) context.Response.Cache.SetExpires(headers.Expires);
            //Sets the Last-Modifed: header
            if (headers.LastModified != DateTime.MinValue) context.Response.Cache.SetLastModified(headers.LastModified);
            //Valid until expires (I.e, ignore refresh requests)
            context.Response.Cache.SetValidUntilExpires(headers.ValidUntilExpires);
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

        protected ApplyResponseHeadersDelegate applyToResponse = null;
        public ApplyResponseHeadersDelegate ApplyToResponse {
            get {
                return applyToResponse;
            }
            set {
                applyToResponse = value;
            }
        }

        protected HttpCacheability cacheControl = HttpCacheability.Private;

        public HttpCacheability CacheControl {
            get { return cacheControl; }
            set { cacheControl = value; }
        }

        protected DateTime expires = DateTime.MinValue;

        public DateTime Expires {
            get { return expires; }
            set { expires = value; }
        }

        protected DateTime lastModified = DateTime.MinValue;

        public DateTime LastModified {
            get { return lastModified; }
            set { lastModified = value; }
        }

        protected bool validUntilExpires = false;

        public bool ValidUntilExpires {
            get { return validUntilExpires; }
            set { validUntilExpires = value; }
        }

        protected bool suppressVaryHeader = true;

        public bool SuppressVaryHeader {
            get { return suppressVaryHeader; }
            set { suppressVaryHeader = value; }
        }


        protected NameValueCollection defaultHeaders = new NameValueCollection();

        public NameValueCollection DefaultHeaders {
            get { return defaultHeaders; }
            set { defaultHeaders = value; }
        }


        protected NameValueCollection headers = new NameValueCollection();

        public NameValueCollection Headers {
            get { return headers; }
            set { headers = value; }
        }

        private List<CacheDependency> dependencies = new List<CacheDependency>();

        public List<CacheDependency> ServerCacheDependencies {
            get { return dependencies; }
            set { dependencies = value; }
        }


    }
}
