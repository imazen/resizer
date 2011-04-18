using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using System.Web;

namespace ImageResizer.Plugins.Basic {
    public enum DiagnosticMode {
        /// <summary>
        /// Diagnostics are disabled
        /// </summary>
        None,
        /// <summary>
        /// Only local requests can access diagnostics
        /// </summary>
        Localhost,
        /// <summary>
        /// All hosts can view diagnostics.
        /// </summary>
        AllHosts

    }
    public class Diagnostic :IPlugin{
        Config c;
        public IPlugin Install(Configuration.Config c) {
            c.Pipeline.PostAuthorizeRequestStart += Pipeline_PostAuthorizeRequestStart;
            c.Plugins.add_plugin(this);
            this.c = c;
            return this;
        }

        void Pipeline_PostAuthorizeRequestStart(System.Web.IHttpModule sender, System.Web.HttpContext context) {
            
            if (context.Request.FilePath.EndsWith("/resizer.info", StringComparison.OrdinalIgnoreCase) &&
                AllowResponse(context)) {
                context.RemapHandler(new DiagnosticPageHandler(c));
            } 
        }

        /// <summary>
        /// True if diagnostics can be displayed to the current user.
        /// If &lt;diagnostics enableFor="None" /&gt;, returns false.
        /// If &lt;diagnostics enableFor="Localhost" /&gt;, returns false for remote requests
        /// If &lt;diagnostics enableFor="AllHosts" /&gt;, returns true.
        /// If unspecified, uses the same behavior as ASP.NET Custom Errors.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool AllowResponse(HttpContext context) {
            
            bool detailedErrors = !context.IsCustomErrorEnabled;
            DiagnosticMode def = (detailedErrors) ? DiagnosticMode.AllHosts : DiagnosticMode.None;

            DiagnosticMode mode = c.get<DiagnosticMode>("diagnostics.enableFor", def);
            if (mode == DiagnosticMode.None) return false;
            if (mode == DiagnosticMode.AllHosts) return false;
            return context.Request.IsLocal;
            
        }

        public bool Uninstall(Configuration.Config c) {
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            return true;
        }
    }
}
