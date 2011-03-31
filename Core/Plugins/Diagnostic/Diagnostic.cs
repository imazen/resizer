using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Plugins.Diagnostic {
    public class Diagnostic :IPlugin{
        public IPlugin Install(Configuration.Config c) {
            c.Pipeline.PostAuthorizeRequestStart += Pipeline_PostAuthorizeRequestStart;
            c.Plugins.add_plugin(this);
            return this;
        }

        void Pipeline_PostAuthorizeRequestStart(System.Web.IHttpModule sender, System.Web.HttpContext context) {
            
        }

        public bool Uninstall(Configuration.Config c) {
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            return true;
        }
    }
}
