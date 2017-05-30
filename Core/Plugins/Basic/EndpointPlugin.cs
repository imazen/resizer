using ImageResizer.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ImageResizer.Plugins.Basic
{


    public class EndpointPlugin : IPlugin
    {
        Config c;

        public IPlugin Install(Configuration.Config c)
        {
            // Only Config.Current ever receives PostAuthorizeRequestStart. 
            // No need for deduplication here
            c.Pipeline.PostAuthorizeRequestStart += Pipeline_PostAuthorizeRequestStart;
            c.Plugins.add_plugin(this);
            this.c = c;
            return this;
        }

        protected string[] Endpoints { get; set; }

        protected EndpointMatching EndpointMatchMethod { get; set; } = EndpointMatching.FilePathMatchesOrdinal;

        protected enum EndpointMatching
        {
            FilePathEndsWithOrdinalIgnoreCase,
            FilePathMatchesOrdinal
        }

        protected  virtual string GenerateOutput(HttpContext context, Config c) { return ""; }

        protected virtual bool HandlesRequest(HttpContext context, Config c)
        {
            switch (EndpointMatchMethod) {
                case EndpointMatching.FilePathEndsWithOrdinalIgnoreCase:
                    return Endpoints.Any(v => context.Request.FilePath.EndsWith(v, StringComparison.OrdinalIgnoreCase));
                case EndpointMatching.FilePathMatchesOrdinal:
                    return Endpoints.Any(v => context.Request.FilePath.Equals(v, StringComparison.Ordinal));
                default:
                    return false;
            }
        }

        protected virtual void ProcessRequest(HttpContext context)
        {

            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Write(GenerateOutput(context, c));
        }


        void Pipeline_PostAuthorizeRequestStart(System.Web.IHttpModule sender, System.Web.HttpContext context)
        {
            if (HandlesRequest(context, c)) {
                //Communicate to the MVC plugin this request should not be affected by the UrlRoutingModule.
                context.Items[c.Pipeline.StopRoutingKey] = true;
                //Provide the request handler
                context.RemapHandler(new EndpointPluginPageHandler(ProcessRequest));
            }
        }

        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            return true;
        }

        class EndpointPluginPageHandler : IHttpHandler
        {
            readonly Action<HttpContext> respond;
            public EndpointPluginPageHandler(Action<HttpContext> respond) { this.respond = respond; }

            public bool IsReusable => false;

            public void ProcessRequest(HttpContext context) { respond(context); }
        }

    }
}
