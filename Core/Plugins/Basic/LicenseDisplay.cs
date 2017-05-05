using ImageResizer.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ImageResizer.Plugins.Basic
{
    /// <summary>
    /// Provides the /resizer.license page
    /// </summary>
    public class LicenseDisplay : IPlugin
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

        public static string GetPageText(Config c)
        {
            return string.Join("\n\n",
                c.Plugins.GetAll<ILicenseDiagnosticsProvider>()
                .Select(p => p.ProvidePublicText())
                .Distinct());
        }

        void Pipeline_PostAuthorizeRequestStart(System.Web.IHttpModule sender, System.Web.HttpContext context)
        {

            if ((context.Request.FilePath.EndsWith("/resizer.license", StringComparison.OrdinalIgnoreCase) ||
                context.Request.FilePath.EndsWith("/resizer.license.ashx", StringComparison.OrdinalIgnoreCase)))
            {
                //Communicate to the MVC plugin this request should not be affected by the UrlRoutingModule.
                context.Items[c.Pipeline.StopRoutingKey] = true;
                //Provide the request handler
                context.RemapHandler(new LicenseDisplayPageHandler(c));
            }
        }


        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            return true;
        }

        private class LicenseDisplayPageHandler : IHttpHandler
        {
            private Config c;

            public LicenseDisplayPageHandler(Config c)
            {
                this.c = c;
            }

            public bool IsReusable
            {
                get { return false; }
            }

            public void ProcessRequest(HttpContext context)
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/plain";
                context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                context.Response.Write(GenerateOutput(context, c));
            }

            public string GenerateOutput(HttpContext context, Config c)
            {
                return LicenseDisplay.GetPageText(c);
            }
        }
    }
}
