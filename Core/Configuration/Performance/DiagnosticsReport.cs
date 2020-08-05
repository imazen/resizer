// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

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
using System.IO;
using System.Linq;
using System.Security;
using ImageResizer.Plugins;
using ImageResizer.Plugins.Basic;
using System.Collections.Specialized;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Configuration.Performance
{
    class DiagnosticsReport
    {
        readonly Config c;
        readonly HttpContext httpContext;

        public DiagnosticsReport(Config c, HttpContext httpContext)
        {
            this.c = c;
            this.httpContext = httpContext;
        }

        IEnumerable<T> GetProviders<T>() where T: class => c.Plugins.GetAll<T>()
                                             .Concat(c.Plugins.GetAll<IDiagnosticsProviderFactory>()
                                                      .Select(
                                                          f => f.GetDiagnosticsProvider() as
                                                              T))
                                             .Where(p => p != null);


        internal string Header()
        {
            //Get loaded assemblies for later use
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var now = DateTime.UtcNow.ToString(NumberFormatInfo.InvariantInfo);
            var distinctCommits = Enumerable.Repeat(GetType().Assembly.GetShortCommit(), 1)
                                            .Concat(assemblies.Select(a => a.GetShortCommit()))
                                            .Where(s => s != null)
                                            .Distinct()
                                            .ToArray();

            var distinctVersions = Enumerable.Repeat(GetType().Assembly, 1)
                                             .Concat(assemblies)
                                             .Where(a => a.GetShortCommit() != null)
                                             .Select(a => a.GetInformationalVersion())
                                             .Where(s => s != null)
                                             .Distinct()
                                             .ToArray();


            return $"Diagnostics for ImageResizer {distinctVersions.Delimited(", ")} {distinctCommits.Delimited(", ")} at {httpContext?.Request.Url.DnsSafeHost} generated {now} UTC";
        }

        internal string Generate()
        {
            //Get loaded assemblies for later use
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var sb = new StringBuilder(8096);
            sb.AppendLine(Header());
            sb.AppendLine("Please remember to provide this page when contacting support.");
            sb.AppendLine();

            sb.AppendLine(GetProviders<IDiagnosticsHeaderProvider>()
                .Select(p => p.ProvideDiagnosticsHeader())
                 .Distinct().Delimited("\r\n\r\n"));

            sb.AppendLine($"You are using ImageResizer {GetEdition() ?? "Essential Edition"} plugins.\r\n");

            if (assemblies.Any(a => a.FullName.Contains("PdfRenderer")))
            {
                sb.AppendLine(
                    "Note: You are using a GPL'd assembly. Consult the PdfRenderer licensing at http://imageresizing.net/licenses/pdfrenderer\r\n");
            }

            var issues = new List<IIssue>(c.AllIssues.GetIssues());
            CheckClassicPipeline(issues);
            CheckForMismatchedVersions(issues, assemblies);

            sb.AppendLine($"{issues.Count} issues detected:\r\n");
            foreach (var i in issues.OrderBy(i => i?.Severity))
                sb.AppendLine($"{i?.Source}({i?.Severity}):\t{i?.Summary}\n\t\t\t{i?.Details?.Replace("\n", "\r\n\t\t\t")}\n");

            sb.AppendLine();
            sb.AppendLine(GetProviders<IDiagnosticsProvider>()
                .Select(p => p.ProvideDiagnostics())
                .Distinct().Delimited("\n"));


            sb.AppendLine("\nConfiguration:\n");
            sb.AppendLine(RedactedConfigXml());


            sb.AppendLine("\nRegistered plugins:\n");
            foreach (var p in c.Plugins.AllPlugins)
                sb.AppendLine(p.ToString());

            sb.AppendLine("\nAccepted querystring keys:\n");
            sb.AppendLine(string.Join(", ", c.Pipeline.SupportedQuerystringKeys));

            sb.AppendLine("\nAccepted file extensions:\n");
            sb.AppendLine(string.Join(", ", c.Pipeline.AcceptedImageExtensions));

            //Echo server assembly, IIS version, OS version, and CLR version.
            sb.AppendLine("\nEnvironment information:\n");
            sb.AppendLine(
                $"Running {ServerSoftware() ?? "NOT ASP.NET"} on {Environment.OSVersion} and CLR {Environment.Version}");
            sb.AppendLine("Trust level: " + ProcessInfo.GetCurrentTrustLevel());

            try {
                var wow64 = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
                var arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                sb.AppendLine("OS bitness: " + arch + (string.IsNullOrEmpty(wow64)
                                  ? ""
                                  : " !! Warning, running as 32-bit on a 64-bit OS(" + wow64 +
                                    "). This will limit ram usage !!"));
            } catch (SecurityException) {
                sb.AppendLine(
                    "Failed to detect operating system bitness - security restrictions prevent reading environment variables");
            }

            // PROCESSOR_ARCHITECTURE	x86	AMD64	x86
            // PROCESSOR_ARCHITEW6432	undefined	undefined	AMD64

            if (ProcessInfo.HasFullTrust()) {
                sb.AppendLine("Executing assembly: " + ProcessInfo.MainModuleFileName());
            }

            sb.AppendLine("IntegratedPipeline: " + HttpRuntime.UsingIntegratedPipeline);

           
            var modules = httpContext?.ApplicationInstance?.Modules;
            if (modules != null)
            {
                sb.AppendLine("\nInstalled HttpModules: \n");
                foreach (var key in modules.AllKeys) {
                    var name = modules.Get(key).GetType().AssemblyQualifiedName;
                    sb.AppendLine($"{name}          (under key {key})");
                }
            }


            //List loaded assemblies, and also detect plugin assemblies that are not being used.
            sb.AppendLine("\nLoaded assemblies:\n");

            var unusedPlugins = new StringBuilder();
            var usedAssemblies = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in c.Plugins.AllPlugins)
                usedAssemblies[p.GetType().Assembly.FullName] = true;

            foreach (var a in assemblies) {
                var assemblyName = new AssemblyName(a.FullName);
                var line = "";
                var error = a.GetExceptionForReading<AssemblyFileVersionAttribute>();
                if (error != null) {
                    line += $"{assemblyName.Name,-40} Failed to read assembly attributes: {error.Message}";
                } else {
                    var version = $"{a.GetFileVersion()} ({assemblyName.Version})";
                    var infoVersion = $"{a.GetInformationalVersion()} {a.GetShortCommit()}";
                    line += $"{assemblyName.Name,-40} File: {version,-25} Informational: {infoVersion,-30} {LookupEdition(a.GetEditionCode())}";
                }


                if (assemblyName.Name.StartsWith("ImageResizer.Plugins", StringComparison.OrdinalIgnoreCase) &&
                    !usedAssemblies.ContainsKey(a.FullName)) {
                    unusedPlugins.AppendLine(line);
                }
                sb.AppendLine(line);
            }

            if (unusedPlugins.Length > 0) {
                sb.AppendLine("\nThe following plugin assemblies are loaded but do not seem to be in use. " +
                              "You should remove them (and their unused dependencies) from the /bin folder to improve application load times:\n");
                sb.Append(unusedPlugins);

                sb.AppendLine(
                    "\nReference list of plugin dependencies - so you know what additional DLLs to remove when removing a plugin. (may not be up-to-date, see plugin docs):\n");
                sb.AppendLine(
                    "The FreeImage plugin has the following dependencies: FreeImage.dll and FreeImageNET.dll\n" +
                    "The Logging plugin depends on: NLog.dll\n" +
                    "The AdvancedFilters, RedEye, and WhitespaceTrimmer plugins depend on: AForge.dll, AForge.Math.dll, and AForge.Imaging.dll\n" +
                    "The PsdReader and PsdComposer plugins depend on: PsdFile.dll\n" +
                    "The S3Reader plugin depends on: AWSSDK.dll\n" +
                    "The BatchZipper plugin depends on: Ionic.Zip.Reduced.dll\n" +
                    "The PdfRenderer plugin depends on gsdll32.dll or gdsll32.dll\n" +
                    "The Faces and RedEye plugins depend on several dozen files... see the plugin docs.\n");
            }

            sb.AppendLine(GetProviders<IDiagnosticsFooterProvider>()
                .Select(p => p.ProvideDiagnosticsFooter())
                .Distinct().Delimited("\n"));

            sb.AppendLine(
                "\n\nWhen fetching a remote license file (if you have one), the following information is sent via the querystring.");
            foreach (var pair in GetReportedPairs().GetInfo()) {
                sb.AppendFormat("   {0,32} {1}\n", pair.Key, pair.Value);
            }

            return sb.ToString();

        }

        static IInfoAccumulator GetReportedPairs()
        {
            var info = GlobalPerf.Singleton.GetReportPairs().WithPrepend(true);

            info.AddString("first_heartbeat", "[integer]");
            info.AddString("new_heartbeats", "[integer]");
            info.AddString("total_heartbeats", "[integer]");
            info.AddString("manager_id", "[guid]");
            info.AddString("license_id", "[integer]");
            return info;
        }

     
        Dictionary<string, List<string>> GetImageResizerVersions(Assembly[] assemblies)
        {

            //Verify we're using the same file version of all ImageResizer assemblies.
            var versions = new Dictionary<string, List<string>>();
            foreach (var a in assemblies) {
                var is_imazen_assembly = a.GetFirstAttribute<AssemblyCopyrightAttribute>()?.Copyright?.Contains("Imazen") == true;

                var an = new AssemblyName(a.FullName);
                if (!an.Name.StartsWith("ImageResizer", StringComparison.OrdinalIgnoreCase) || !is_imazen_assembly) {
                    continue;
                }
                var key = a.GetFileVersion() ?? an.Version.Major + "." + an.Version.Minor + "." + an.Version.Build;
                if (!versions.ContainsKey(key)) versions[key] = new List<string>();
                versions[key].Add(an.Name);
            }
            return versions;
        }

        readonly Dictionary<string, string> friendlyEditionNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                {"R4Elite", "Elite Edition"},
                {"R4Creative", "Creative Edition"},
                {"R4Performance", "Performance Edition"},
                {"R_Elite", "Elite Edition"},
                {"R_Creative", "Creative Edition"},
                {"R_Performance", "Performance Edition"}
            };

        internal string GetEdition()
        {

            //What edition is used?
            var largestEdition = c.Plugins.GetAll<ILicensedPlugin>()
                                  .SelectMany(p => p.LicenseFeatureCodes)
                                  .Concat(
                                      c.Plugins.AllPlugins.Select(p => p
                                          .GetType()
                                          .Assembly.GetFirstAttribute<EditionAttribute>()
                                          ?.Value))

                                  .FirstOrDefault(
                                      s => friendlyEditionNames.Keys.Contains(s, StringComparer.OrdinalIgnoreCase));
            ;

            return LookupEdition(largestEdition);
        }

        string LookupEdition(string editionCode) => editionCode == null
            ? null
            : (friendlyEditionNames.ContainsKey(editionCode)
                ? friendlyEditionNames[editionCode]
                : editionCode);



        string RedactedConfigXml()
        {
            //Let plugins redact sensitive information from the configuration before we display it
            return c.Plugins.GetAll<IRedactDiagnostics>()
                    .Aggregate(c.getConfigXml(), (current, d) => d.RedactFrom(current))?
                    .RedactAttributes("remoteReader", new[] {"signingKey"})
                    .RedactAttributes("plugins.add", new[] {"connectionString", "accessKeyId", "secretAccessKey"})
                    .ToString();
        }

    


        string ServerSoftware() => httpContext?.Request.ServerVariables["SERVER_SOFTWARE"];

        void CheckClassicPipeline(List<IIssue> issues)
        {
            if (!HttpRuntime.UsingIntegratedPipeline &&
                ServerSoftware()?.IndexOf("IIS", 0, StringComparison.OrdinalIgnoreCase) > -1)
            {
                var ext = c.Pipeline.FakeExtensions.Count > 0
                    ? c.Pipeline.FakeExtensions[0]
                    : "[the fakeExtensions attribute of the <pipeline> element in web.config is empty. Remove, or set to .ashx]";
                issues.Add(new Issue("Pipeline",
                    "This app is running in Classic mode instead of Integrated mode. This causes reduced performance and requires a special URL syntax." +
                    "In classic mode, you will need to append the " + ext +
                    " extension to any images you wish to process.\n" +
                    "Alternatively, switch the mode to Integrated in the application's App Pool.",
                    IssueSeverity.Warning));

            }
        }

        void CheckForMismatchedVersions(List<IIssue> issues, Assembly[] assemblies)
        {
            //Verify we're using the same general version of all ImageResizer assemblies.
            var versions = GetImageResizerVersions(assemblies);
            if (versions.Count > 1)
            {
                issues.Add(new Issue("Potentially incompatible ImageResizer assemblies were detected.",
                    "Please make sure all ImageResizer assemblies are from the same version. Compatibility issues are very likely if you mix plugins from different releases: " +
                    versions.Select(pair => $"{pair.Key} assemblies: {pair.Value.Delimited(", ")}").Delimited("\n"), IssueSeverity.Warning));

            }
        }
    }
}
