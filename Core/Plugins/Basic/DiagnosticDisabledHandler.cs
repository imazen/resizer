using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;

namespace ImageResizer.Plugins.Basic {
    public class DiagnosticDisabledHandler : IHttpHandler {
        Config c;
        public DiagnosticDisabledHandler(Config c) {
            this.c = c;
        }

        public bool IsReusable {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context) {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Write(GenerateOutput(context, c));
        }

        public string GenerateOutput(HttpContext context, Config c) {
            StringBuilder sb = new StringBuilder();

            //Figure out CustomErrorsMode
            System.Configuration.Configuration configuration = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(null);
            CustomErrorsSection section = (configuration != null) ? section = (CustomErrorsSection)configuration.GetSection("system.web/customErrors") : null;
            CustomErrorsMode mode = (section != null) ? section.Mode : CustomErrorsMode.RemoteOnly;
            //What is diagnostics enableFor set to?
            DiagnosticMode dmode = c.get<DiagnosticMode>("diagnostics.enableFor", DiagnosticMode.None);
            //Is it set all all?
            bool diagDefined = (c.get("diagnostics.enableFor",null) != null);
            //Is it available from localhost.
            bool availLocally = (!diagDefined && mode == CustomErrorsMode.RemoteOnly) || (dmode == DiagnosticMode.Localhost);

            sb.AppendLine("The Resizer diagnostics page is " + (availLocally ? "only available from localhost." : "disabled."));
            sb.AppendLine();
            if (diagDefined) sb.AppendLine("This is because <diagnostics enableFor=\"" + dmode.ToString() + "\" />.");
            else sb.AppendLine("This is because <customErrors mode=\"" + mode.ToString() + "\" />.");
            sb.AppendLine();
            sb.AppendLine("To override for localhost access, add <diagnostics enableFor=\"localhost\" /> in the <resizer> section of Web.config.");
            sb.AppendLine();
            sb.AppendLine("To ovveride for remote access, add <diagnostics enableFor=\"allhosts\" /> in the <resizer> section of Web.config.");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
