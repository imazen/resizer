using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Caching;
using System.Web;

namespace ImageResizer.Plugins.Etags {
    public class EtagsPlugin:IPlugin, ICache {

        Config c;
        public IPlugin Install(Config c) {
            this.c = c;
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public bool CanProcess(System.Web.HttpContext current, IResponseArgs e) {
            string match = GetUnquotedEtag(current);
            
            string eTag = CalcualteETag(e);
            string quotedTag = "\"" + eTag + "\"";
            e.ResponseHeaders.Headers["X-ETag"] = quotedTag;
            e.ResponseHeaders.Headers["ETag"] = quotedTag;
            return (match != null && match.Equals(eTag));
        }

        public void Process(System.Web.HttpContext current, IResponseArgs e) {
            current.RemapHandler(new Return304Handler(e));
        }

        public static string CalcualteETag(IResponseArgs e) {
            return e.RequestKey.GetHashCode().ToString("x");
        }

        /// <summary>
        /// Parses the If-None-Match HTTP header from the given context, stripping quotes and the W/ prefix. Returns null unless the string contains 1 or more characters.
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        public string GetUnquotedEtag(HttpContext current) {
            if (current == null) return null;
            string ifNoneMatch = current.Request.Headers["If-None-Match"];
            if (string.IsNullOrEmpty(ifNoneMatch)) return null;
            if (ifNoneMatch.StartsWith("W/"))ifNoneMatch = ifNoneMatch.Substring(2);
            ifNoneMatch = ifNoneMatch.Trim('"');
            return string.IsNullOrEmpty(ifNoneMatch) ? null : ifNoneMatch;
        }

    }
    public class Return304Handler : IHttpHandler {
        private IResponseArgs e;

        public Return304Handler(IResponseArgs e) {
            this.e = e;
        }

        public bool IsReusable {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context) {
            context.Response.StatusCode = 304;
            context.Response.StatusDescription = "Not Modified";
            context.Response.BufferOutput = true; //Same as .Buffer. Allows bitmaps to be disposed quicker.
            e.ResponseHeaders.ApplyDuringPreSendRequestHeaders = false;
            e.ResponseHeaders.ApplyToResponse(e.ResponseHeaders, context);
            
        }
    }
}
