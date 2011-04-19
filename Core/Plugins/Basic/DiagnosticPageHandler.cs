/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using System.Web.Hosting;
using System.Reflection;

namespace ImageResizer.Plugins.Basic {
    public class DiagnosticPageHandler : IHttpHandler {
        Config c;
        public DiagnosticPageHandler(Config c) {
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
            List<IIssue> issues = new List<IIssue>(c.AllIssues.GetIssues());
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Image resizier diagnostic sheet\t\t" + DateTime.UtcNow.ToString() + "\n");
            sb.AppendLine(issues.Count + " Issues detected:\n");
            foreach (IIssue i in issues)
                sb.AppendLine(i.Source + "(" + i.Severity.ToString() + "):\t" + i.Summary  +
                    ("\n" + i.Details).Replace("\n","\n\t\t\t") + "\n");

            sb.AppendLine("\nRegistered plugins:\n");
            foreach (IPlugin p in c.Plugins.AllPlugins)
                sb.AppendLine(p.ToString());

            sb.AppendLine("\nConfiguration:\n");
            sb.AppendLine(c.getConfigXml().ToString());

            //Echo server assembly, iis version, OS version, and CLR version.
            sb.AppendLine("\nEnvironment information:\n");
            string iis = context.Request.ServerVariables["SERVER_SOFTWARE"];
            if (!string.IsNullOrEmpty(iis)) iis += " on ";
            sb.AppendLine("Running " + iis +
                System.Environment.OSVersion.ToString() + " and CLR " +
                System.Environment.Version.ToString());
            sb.AppendLine("Executing assembly: " + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            sb.AppendLine("IntegratedPipeline=" + (HttpRuntime.UsingIntegratedPipeline).ToString());


            sb.AppendLine("\nLoaded assemblies:\n");
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in asms) {
                sb.AppendLine(a.FullName);
            }
            return sb.ToString();

        }
    }
}
