using System;
using System.Linq;
using System.Web;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.Basic
{
    public class EndpointPlugin : IPlugin
    {
        private Config c;

        public IPlugin Install(Config c)
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

        protected virtual string GenerateOutput(HttpContext context, Config c)
        {
            return "";
        }

        protected virtual bool HandlesRequest(HttpContext context, Config c)
        {
            switch (EndpointMatchMethod)
            {
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
            context.Response.Headers.Add("X-Robots-Tag", "none");
            context.Response.Write(GenerateOutput(context, c));
        }


        private void Pipeline_PostAuthorizeRequestStart(IHttpModule sender, HttpContext context)
        {
            if (HandlesRequest(context, c))
            {
                //Communicate to the MVC plugin this request should not be affected by the UrlRoutingModule.
                context.Items[c.Pipeline.StopRoutingKey] = true;
                //Provide the request handler
                context.RemapHandler(new EndpointPluginPageHandler(ProcessRequest));
            }
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            return true;
        }

        private class EndpointPluginPageHandler : IHttpHandler
        {
            private readonly Action<HttpContext> respond;

            public EndpointPluginPageHandler(Action<HttpContext> respond)
            {
                this.respond = respond;
            }

            public bool IsReusable => false;

            public void ProcessRequest(HttpContext context)
            {
                respond(context);
            }
        }
    }
}