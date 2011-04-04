using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.Diagnostic {
    public class Diagnostic :IPlugin{
        Config c;
        public IPlugin Install(Configuration.Config c) {
            c.Pipeline.PostAuthorizeRequestStart += Pipeline_PostAuthorizeRequestStart;
            c.Plugins.add_plugin(this);
            this.c = c;
            return this;
        }

        void Pipeline_PostAuthorizeRequestStart(System.Web.IHttpModule sender, System.Web.HttpContext context) {
            if (context.Request.FilePath.EndsWith("/resizer.info", StringComparison.OrdinalIgnoreCase)) {
                context.RemapHandler(new DiagnosticPageHandler(c));
            } 
        }

        public bool Uninstall(Configuration.Config c) {
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            return true;
        }
    }
}
