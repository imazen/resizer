using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Web;
using System.Web.Hosting;
using Microsoft.Win32;
using Imazen.Common.Instrumentation.Support;
using Imazen.Common.Instrumentation.Support.InfoAccumulators;

namespace ImageResizer.Configuration.Performance
{
    internal class ProcessInfo
    {
        public static Guid ProcessGuid { get; } = Guid.NewGuid();

        public static int ProcessId { get; } = Process.GetCurrentProcess().Id;

        public bool Process64Bit { get; } = Environment.Is64BitProcess;

        public string DotNetVersionInstalled { get; } = Get45PlusFromRegistry();

        private string IisVer { get; } = GetIisVerStringFromRegistry();

        private bool IntegratedPipeline { get; } = HttpRuntime.UsingIntegratedPipeline;

        private IEnumerable<string> HttpModules { get; set; }

        public void SetModules(HttpModuleCollection col)
        {
            HttpModules = col != null ? GetHttpModules(col) : HttpModules;
        }

        public ProcessInfo()
        {
        }

        public void Add(IInfoAccumulator query)
        {
            var q = query.WithPrefix("proc_");
            q.Add("64", Process64Bit);
            q.Add("guid", ProcessGuid);
            q.Add("sys_dotnet", DotNetVersionInstalled);
            q.Add("iis", IisVer);
            q.Add("integrated_pipeline", IntegratedPipeline);
            q.Add("id_hash", Utilities.Sha256TruncatedBase64(ProcessId.ToString(), 6));

            q.Add("asyncmodule", Config.Current.Pipeline.UsingAsyncMode);
            q.Add("default_commands", Config.Current.get("pipeline.defaultCommands", null));
            q.Add("working_set_mb", Environment.WorkingSet / 1000000);
            q.Add("git_commit", Assembly.GetAssembly(GetType()).GetShortCommit());
            q.Add("info_version", Assembly.GetAssembly(GetType()).GetInformationalVersion());
            q.Add("file_version", Assembly.GetAssembly(GetType()).GetFileVersion());

            if (HostingEnvironment.ApplicationPhysicalPath != null)
                q.Add("apppath_hash", Utilities.Sha256TruncatedBase64(HostingEnvironment.ApplicationPhysicalPath, 6));

            // Add HttpModule class names without prefixing
            SetModules(HttpContext.Current?.ApplicationInstance?.Modules);
            if (HttpModules != null)
                foreach (var name in HttpModules)
                    query.Add("mod", name);


            // TODO: check for mismatched assemblies?
        }

        private static Tuple<int?, int?> GetIisVerFromRegistry()
        {
            try
            {
                const string subkey = @"SOFTWARE\Microsoft\InetStp\";
                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                           .OpenSubKey(subkey))
                {
                    //SetupString
                    return new Tuple<int?, int?>(ndpKey?.GetValue("MajorVersion") as int?,
                        ndpKey?.GetValue("MinorVersion") as int?);
                }
            }
            catch
            {
            }

            return null;
        }

        private static string GetIisVerStringFromRegistry()
        {
            var pair = GetIisVerFromRegistry();
            if (pair?.Item1 != null && pair.Item2 != null) return $"{pair.Item1}.{pair.Item2}";
            return null;
        }

        private static string Get45PlusFromRegistry()
        {
            try
            {
                const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                           .OpenSubKey(subkey))
                {
                    // Version
                    if (ndpKey?.GetValue("Release") != null)
                        return CheckFor45PlusVersion((int)ndpKey.GetValue("Release"));
                }
            }
            catch
            {
            }

            return null;
        }

        // Checking the version using >= will enable forward compatibility.
        private static string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 460798) return "4.7 or later";
            if (releaseKey >= 394802) return "4.6.2";
            if (releaseKey >= 394254) return "4.6.1";
            if (releaseKey >= 393295) return "4.6";
            if (releaseKey >= 379893) return "4.5.2";
            if (releaseKey >= 378675) return "4.5.1";
            if (releaseKey >= 378389) return "4.5";
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
        }

        private static IEnumerable<string> GetHttpModules(HttpModuleCollection modules)
        {
            if (modules != null)
                return modules.AllKeys.Select(key =>
                        modules.Get(key).GetType().FullName
                            .Replace("System.Web.Security", "").Replace("System.Web", ""))
                    .ToArray();
            return Enumerable.Empty<string>();
        }

        /// <summary>
        ///     Returns the ASP.NET trust level
        /// </summary>
        /// <returns></returns>
        public static AspNetHostingPermissionLevel GetCurrentTrustLevel()
        {
            foreach (var trustLevel in
                     new[]
                     {
                         AspNetHostingPermissionLevel.Unrestricted,
                         AspNetHostingPermissionLevel.High,
                         AspNetHostingPermissionLevel.Medium,
                         AspNetHostingPermissionLevel.Low,
                         AspNetHostingPermissionLevel.Minimal
                     })
            {
                try
                {
                    new AspNetHostingPermission(trustLevel).Demand();
                }
                catch (SecurityException)
                {
                    continue;
                }

                return trustLevel;
            }

            return AspNetHostingPermissionLevel.None;
        }

        public static string MainModuleFileName()
        {
            try
            {
                return Process.GetCurrentProcess().MainModule.FileName;
            }
            catch (Win32Exception)
            {
                return "(process filename cannot be determined, access denied)";
            }
        }

        public static bool HasFullTrust()
        {
            try
            {
                new AspNetHostingPermission(AspNetHostingPermissionLevel.Unrestricted).Demand();
                return true;
            }
            catch (SecurityException)
            {
            }

            return false;
        }
    }
}