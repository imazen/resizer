/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using System.Web.Hosting;
using System.Reflection;
using ImageResizer.Util;

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


			sb.AppendLine("\nAccepted querystring keys:\n");
			foreach (string s in c.Pipeline.SupportedQuerystringKeys) {
				sb.Append(s + ", ");
			}
			sb.AppendLine();

			sb.AppendLine("\nAccepted file extensions:\n");
			foreach (string s in c.Pipeline.AcceptedImageExtensions) {
				sb.Append(s + ", ");
			}
			sb.AppendLine();


			//Echo server assembly, iis version, OS version, and CLR version.
			sb.AppendLine("\nEnvironment information:\n");
			string iis = context != null ? context.Request.ServerVariables["SERVER_SOFTWARE"] : "NOT ASP.NET";
			if (!string.IsNullOrEmpty(iis)) iis += " on ";
			sb.AppendLine("Running " + iis +
				System.Environment.OSVersion.ToString() + " and CLR " +
				System.Environment.Version.ToString());


			if (hasFullTrust()) {
				sb.AppendLine("Executing assembly: " + mainModuleFileName());
			}

			sb.AppendLine("IntegratedPipeline=" + (HttpRuntime.UsingIntegratedPipeline).ToString());


			sb.AppendLine("\nLoaded assemblies:\n");
			Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly a in asms) {
				AssemblyName assemblyName = new AssemblyName(a.FullName);
				sb.Append(assemblyName.Name.PadRight(40, ' '));

				sb.Append(" Assembly: " + assemblyName.Version.ToString().PadRight(15));



                object[] attrs;
                
				attrs = a.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				if (attrs != null && attrs.Length > 0) sb.Append(" File: " + ((AssemblyFileVersionAttribute)attrs[0]).Version.PadRight(15));

				attrs = a.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
				if (attrs != null && attrs.Length > 0) sb.Append(" Info: " + ((AssemblyInformationalVersionAttribute)attrs[0]).InformationalVersion);

				attrs = a.GetCustomAttributes(typeof(CommitAttribute), false);
				if (attrs != null && attrs.Length > 0) sb.Append("  Commit: " + ((CommitAttribute)attrs[0]).Value);

				sb.AppendLine();
			}
			return sb.ToString();

		}

		private static string mainModuleFileName() {
			return System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
		}

		private static bool hasFullTrust() {
			bool fullTrust = false;
			try
			{
				new AspNetHostingPermission(AspNetHostingPermissionLevel.Unrestricted).Demand();
				fullTrust = true;
			}
			catch (System.Security.SecurityException)
			{
			}
			return fullTrust;
		}

	}
}
