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
using System.ComponentModel;
using ImageResizer.Configuration.Xml;
using System.Globalization;

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
            //Get loaded assemblies for later use
			Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();


			List<IIssue> issues = new List<IIssue>(c.AllIssues.GetIssues());
            
            //Verify we are using MvcRoutingShim if System.Web.Routing is loaded.
            bool routingLoaded = false;
			foreach (Assembly a in asms) {
                if (new AssemblyName(a.FullName).Name.StartsWith("System.Web.Routing", StringComparison.OrdinalIgnoreCase)) {
                    routingLoaded = true; break;
                }
            }
            bool usingRoutingShim = false;
            foreach (IPlugin p in c.Plugins.AllPlugins){
                if (p.GetType().Name.EndsWith("MvcRoutingShimPlugin", StringComparison.OrdinalIgnoreCase)) {
                    usingRoutingShim = true; break;
                }
            }

            if (routingLoaded && !usingRoutingShim) issues.Add(new Issue(
                "The MvcRoutingShim plugin must be installed if you are using MVC or System.Web.Routing",
                "System.Web.Routing is loaded. You must install the MvcRoutingShim plugin. Add ImageResizer.Mvc.dll to your project, and add '<add name=\"MvcRoutingPlugin\" />' to the <plugins> section of web.config.", IssueSeverity.Critical));

            if (!HttpRuntime.UsingIntegratedPipeline && context != null && context.Request != null) {
                string server = context.Request.ServerVariables["SERVER_SOFTWARE"];
                string ext = c.Pipeline.FakeExtensions.Count > 0 ? c.Pipeline.FakeExtensions[0] : "[the fakeExtensions attribute of the <pipeline> element in web.config is empty. Remove, or set to .ashx]";

                if (server.IndexOf("IIS/7", 0, StringComparison.OrdinalIgnoreCase) > -1) {
                    issues.Add(new Issue("Pipeline", "This app is running in Classic mode instead of Integrated mode. This causes reduced performance and requires a special URL syntax." + 
						"In classic mode, you will need to append the " + ext + " extension to any images you wish to process.\n" +
                        "Alternatively, switch the mode to Integrated in the application's App Pool.", IssueSeverity.Warning));
                }
            }

            //Verify we're using the same general version of all ImageResizer assemblies.
            Dictionary<string, List<string>> versions = new Dictionary<string, List<string>>();
            foreach (Assembly a in asms) {
                AssemblyName an = new AssemblyName(a.FullName);
                if (an.Name.StartsWith("ImageResizer", StringComparison.OrdinalIgnoreCase)) {
                    string key = an.Version.Major + "." + an.Version.Minor + "." + an.Version.Build;
                    if (!versions.ContainsKey(key)) versions[key] = new List<string>();
                    versions[key].Add(an.Name);
                }
            }
            if (versions.Keys.Count > 1) {
                string groups = "";
                foreach(string v in versions.Keys){
                    groups += "\n" + v + " assemblies: ";
                    foreach(string a in versions[v])
                        groups += a + ", ";
                    groups = groups.TrimEnd(' ', ',');
                }
                issues.Add(new Issue("Potentially incompatible ImageResizer assemblies were detected.",
                    "Please make sure all ImageResizer assemblies are from the same version. Compatibility issues are possible if you mix plugins from different releases." + groups, IssueSeverity.Warning));

            }

			StringBuilder sb = new StringBuilder();
            sb.AppendLine("Image resizer diagnostic sheet\t\t" + DateTime.UtcNow.ToString(NumberFormatInfo.InvariantInfo) + "\n");
			sb.AppendLine(issues.Count + " Issues detected:\n");
			foreach (IIssue i in issues)
				sb.AppendLine(i.Source + "(" + i.Severity.ToString() + "):\t" + i.Summary  +
					("\n" + i.Details).Replace("\n","\n\t\t\t") + "\n");

            //What bundles are used?
            Dictionary<string, bool> bundlesUsed = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (IPlugin p in c.Plugins.AllPlugins) {
                object[] attrs = p.GetType().Assembly.GetCustomAttributes(typeof(Util.BundleAttribute), true);
                if (attrs.Length > 0 && attrs[0] is BundleAttribute) {
                    bundlesUsed[((BundleAttribute)attrs[0]).Value] = true;
                }
            }
            //Support multiple bundle ownership
            List<string> keys = new List<string>(bundlesUsed.Keys);
            foreach (string s in keys) {
                if (s.IndexOf(',') > -1) {
                    bool found = false;
                    string[] bundles = s.Split(',');
                    foreach (string b in bundles) {
                        if (bundlesUsed.ContainsKey(b.Trim())) {
                            bundlesUsed[b.Trim()] = true;
                            bundlesUsed.Remove(s);
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        //Ok, so none of those bundles are used elsewhere. Use the first bundle listed.
                        bundlesUsed[bundles[0].Trim()] = true;
                        bundlesUsed.Remove(s);
                    }
                }
            }

            Dictionary<string, string> friendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            friendlyNames.Add("R3Bundle1", "Performance Bundle");
            friendlyNames.Add("R3Bundle2", "Design Bundle");
            friendlyNames.Add("R3Bundle3", "Cloud Bundle");
            friendlyNames.Add("R3Bundle4", "Extras Bundle");

            if (bundlesUsed.Count == 0) 
                sb.AppendLine("\nYou are not using any paid bundles.");
            else {
                sb.Append("\nYou are using paid bundles: ");
                foreach (string s in bundlesUsed.Keys) sb.Append((friendlyNames.ContainsKey(s) ? friendlyNames[s] : s) + ", ");
                sb.Remove(sb.Length - 2, 2);
                sb.AppendLine();
            }

			sb.AppendLine("\nRegistered plugins:\n");
			foreach (IPlugin p in c.Plugins.AllPlugins)
				sb.AppendLine(p.ToString());

			sb.AppendLine("\nConfiguration:\n");
            //Start out the signing key. TODO: star out db passwords.
            string config = c.getConfigXml().ToString();
            string pwd = c.get("remoteReader.signingKey", String.Empty);
            if (!string.IsNullOrEmpty(pwd)) config.Replace(pwd,"*********");
			sb.AppendLine(config);


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
            sb.AppendLine("Trust level: " + GetCurrentTrustLevel().ToString());

			if (hasFullTrust()) {
				sb.AppendLine("Executing assembly: " + mainModuleFileName());
			}

			sb.AppendLine("IntegratedPipeline: " + (HttpRuntime.UsingIntegratedPipeline).ToString());

            //List loaded assemblies, and also detect plugin assemblies that are not being used.
			sb.AppendLine("\nLoaded assemblies:\n");

            StringBuilder unusedPlugins = new StringBuilder();
            Dictionary<string, bool> usedAssemblies = new Dictionary<string,bool>(StringComparer.OrdinalIgnoreCase);
            foreach (IPlugin p in c.Plugins.AllPlugins) 
                usedAssemblies[p.GetType().Assembly.FullName] = true;

            
			foreach (Assembly a in asms) {
                StringBuilder asb = new StringBuilder();

				AssemblyName assemblyName = new AssemblyName(a.FullName);
                
				asb.Append(assemblyName.Name.PadRight(40, ' '));

				asb.Append(" Assembly: " + assemblyName.Version.ToString().PadRight(15));

               
                object[] attrs;
                
				attrs = a.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				if (attrs != null && attrs.Length > 0) asb.Append(" File: " + ((AssemblyFileVersionAttribute)attrs[0]).Version.PadRight(15));

				attrs = a.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
				if (attrs != null && attrs.Length > 0) asb.Append(" Info: " + ((AssemblyInformationalVersionAttribute)attrs[0]).InformationalVersion);

				attrs = a.GetCustomAttributes(typeof(CommitAttribute), false);
				if (attrs != null && attrs.Length > 0) asb.Append("  Commit: " + ((CommitAttribute)attrs[0]).Value);

				asb.AppendLine();

                
                if (assemblyName.Name.StartsWith("ImageResizer.Plugins", StringComparison.OrdinalIgnoreCase) && !usedAssemblies.ContainsKey(a.FullName)) {
                    unusedPlugins.Append(asb.ToString());
                }
               
                sb.Append(asb.ToString());
			}

            if (unusedPlugins.Length > 0) {
                sb.AppendLine("\nThe following plugin assemblies are loaded but do not seem to be in use. " +
                    "You should remove them (and especially their dependencies (unless used elsewhere)) from the /bin folder to improve application load times:\n");
                sb.Append(unusedPlugins.ToString());

                sb.AppendLine("\nReference list of plugin dependencies - so you know what additional dlls to remove when removing a plugin. (may not be up-to-date, see plugin docs):\n");
                sb.AppendLine("The FreeImage plugin has the following dependencies: FreeImage.dll and FreeImageNET.dll\n" +
                    "The Logging plugin depends on: NLog.dll\n" +
                    "The AdvancedFilters, RedEye, and WhitespaceTrimmer plugins depend on: AForge.dll, AForge.Math.dll, and AForge.Imaging.dll\n" +
                    "The PsdReader and PsdComposer plugins depend on: PsdFile.dll\n" +
                    "The S3Reader plugin depends on: LitS3.dll\n" +
                    "The BatchZipper plugin depends on: Ionic.Zip.Reduced.dll\n" + 
                    "The PdfRenderer plugin depends on gsdll32.dll or gdsll32.dll\n" + 
                    "The RedEye plugin depends on several dozen files... see the plugin docs.\n");
            }
			return sb.ToString();

		}
        /// <summary>
        /// Returns the ASP.NET trust level
        /// </summary>
        /// <returns></returns>
        AspNetHostingPermissionLevel GetCurrentTrustLevel() {
            foreach (AspNetHostingPermissionLevel trustLevel in
                    new AspNetHostingPermissionLevel[] {
            AspNetHostingPermissionLevel.Unrestricted,
            AspNetHostingPermissionLevel.High,
            AspNetHostingPermissionLevel.Medium,
            AspNetHostingPermissionLevel.Low,
            AspNetHostingPermissionLevel.Minimal 
        }) {
                try {
                    new AspNetHostingPermission(trustLevel).Demand();
                } catch (System.Security.SecurityException) {
                    continue;
                }

                return trustLevel;
            }

            return AspNetHostingPermissionLevel.None;
        }

		private static string mainModuleFileName() {
            try {
                return System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            } catch (Win32Exception) {
                return " (cannot be determined, access denied)";
            }

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
