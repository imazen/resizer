/* Copyright (c) 2014 Imazen See license.txt */
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
using System.Linq;
using System.Security;

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
                "System.Web.Routing is loaded. You must install the MvcRoutingShim plugin. Add ImageResizer.Mvc.dll to your project, and add '<add name=\"MvcRoutingShim\" />' to the <plugins> section of web.config.", IssueSeverity.Critical));

            if (!HttpRuntime.UsingIntegratedPipeline && context != null && context.Request != null) {
                string server = context.Request.ServerVariables["SERVER_SOFTWARE"];
                if (server.IndexOf("IIS", 0, StringComparison.OrdinalIgnoreCase) > -1) {

                    string ext = c.Pipeline.FakeExtensions.Count > 0 ? c.Pipeline.FakeExtensions[0] : "[the fakeExtensions attribute of the <pipeline> element in web.config is empty. Remove, or set to .ashx]";

                    if (server.IndexOf("IIS/7", 0, StringComparison.OrdinalIgnoreCase) > -1 ||
                        server.IndexOf("IIS/8", 0, StringComparison.OrdinalIgnoreCase) > -1) {
                        issues.Add(new Issue("Pipeline", "This app is running in Classic mode instead of Integrated mode. This causes reduced performance and requires a special URL syntax." +
                            "In classic mode, you will need to append the " + ext + " extension to any images you wish to process.\n" +
                            "Alternatively, switch the mode to Integrated in the application's App Pool.", IssueSeverity.Warning));
                    } else {
                        issues.Add(new Issue("Server", server + " does not support Integrated mode or does not have it enabled.",
                            "You must append the " + ext + " extension to any image requests you wish to process.\n", IssueSeverity.Warning));
                    }
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

            //What editions are used?
            var editionsUsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (IPlugin p in c.Plugins.AllPlugins) {
                object[] attrs = p.GetType().Assembly.GetCustomAttributes(typeof(Util.EditionAttribute), true);
                if (attrs.Length > 0 && attrs[0] is EditionAttribute)
                {
                    editionsUsed[p.GetType().Name] = ((EditionAttribute)attrs[0]).Value;
                }
            }

            string edition = null;
            //Pick the largest edition
            foreach (string s in new string[] { "R4Elite", "R4Creative", "R4Performance" })
            {
                if (new List<string>(editionsUsed.Values).Contains(s))
                {
                    edition = s;
                    break;
                }
            }
            
            if (new List<string>(editionsUsed.Values).Intersect(new string[] { "R3Elite", "R3Creative", "R3Performance" }).Count() > 0){
                sb.AppendLine("You are mixing V3 and V4 plugins; this is a bad idea.");
            }
            Dictionary<string, string> friendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            friendlyNames.Add("R4Elite", "V4 Elite Edition or Support Contract");
            friendlyNames.Add("R4Creative", "V4 Creative Edition");
            friendlyNames.Add("R4Performance", "V4 Performance Edition");

            if (edition == null) 
                sb.AppendLine("\nYou are not using any paid plugins.");
            else {
                sb.Append("\nYou are using plugins from the " + friendlyNames[edition] + ": ");
                foreach (string s in editionsUsed.Keys) {
                    sb.Append(s + " (" + (friendlyNames.ContainsKey(editionsUsed[s]) ? friendlyNames[editionsUsed[s]] : "Unrecognized SKU")+ "), ");
                }
                sb.Remove(sb.Length - 2, 2);
                sb.AppendLine();
            }

            sb.AppendLine("\nRegistered plugins:\n");
            foreach (IPlugin p in c.Plugins.AllPlugins)
                sb.AppendLine(p.ToString());

            sb.AppendLine("\nConfiguration:\n");

            //Let plugins redact sensitive information from the configuration before we display it
            Node n = c.getConfigXml();
            foreach (IRedactDiagnostics d in c.Plugins.GetAll<IRedactDiagnostics>())
                n = d.RedactFrom(n);

            string config = n.ToString();
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

            try{
                string wow64 = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
                string arch = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                sb.AppendLine("OS bitness: " + arch + (string.IsNullOrEmpty(wow64) ? "" : " !! Warning, running as 32-bit on a 64-bit OS(" + wow64 + "). This will limit ram usage !!"));
            }catch(SecurityException){
                sb.AppendLine("Failed to detect operating system bitness - security restrictions prevent reading environment variables");
            }

            // PROCESSOR_ARCHITECTURE	x86	AMD64	x86
            // PROCESSOR_ARCHITEW6432	undefined	undefined	AMD64

            if (hasFullTrust()) {
                sb.AppendLine("Executing assembly: " + mainModuleFileName());
            }

            sb.AppendLine("IntegratedPipeline: " + (HttpRuntime.UsingIntegratedPipeline).ToString());

            if (HttpContext.Current != null && HttpContext.Current.ApplicationInstance != null && HttpContext.Current.ApplicationInstance.Modules != null)
            {
                var modules = HttpContext.Current.ApplicationInstance.Modules;
                sb.AppendLine("\nInstalled HttpModules: \n");
                foreach (string key in modules.AllKeys)
                {
                    sb.AppendLine(modules.Get(key).GetType().AssemblyQualifiedName + "          (under key" + key + ")");
                }
            }


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
