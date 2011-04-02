using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using fbs.ImageResizer.Configuration;

namespace fbs.ImageResizer.Plugins.Diagnostic {
    public class DiagnosticPage {
        Config c;
        public DiagnosticPage(Config c) {
            this.c = c;
        }

        public void Send(HttpContext context) {
            context.Response.ContentType = "text/plain";
            
        }
    }
}
