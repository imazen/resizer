using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using Microsoft.Win32;
using System.Web;
using System.Reflection;

namespace ImageResizer.Configuration.Performance
{
    class ProcessInfo
    {
        public static Guid ProcessGuid { get; } = Guid.NewGuid();

        public bool Process64Bit { get; } = Environment.Is64BitProcess;

        public string DotNetVersionInstalled { get; } = Get45PlusFromRegistry();

        string IISVer { get; } = GetIISVerStringFromRegistry();

        bool IntegratedPipeline { get; } = HttpRuntime.UsingIntegratedPipeline;

        IEnumerable<string> HttpModules { get; set; }

        public void SetModules(HttpModuleCollection col)
        {
            HttpModules = col != null ? GetHttpModules(col) : HttpModules;
        }
        public ProcessInfo() { }

        public void Add(IInfoAccumulator query)
        {
            var q = query.WithPrefix("proc_");
            q.Add("64", Process64Bit);
            q.Add("guid", ProcessGuid);
            q.Add("sys_dotnet", DotNetVersionInstalled);
            q.Add("iis", IISVer);
            q.Add("integrated_pipeline", IntegratedPipeline);

            q.Add("asyncmodule", Config.Current.Pipeline.UsingAsyncMode);
            q.Add("default_commands", Config.Current.get("pipeline.defaultCommands", null));
            q.Add("working_set_mb", Environment.WorkingSet / 1000000);
            q.Add("git_commit", Assembly.GetAssembly(this.GetType()).GetShortCommit());
            q.Add("info_version", Assembly.GetAssembly(this.GetType()).GetInformationalVersion());
            q.Add("file_version", Assembly.GetAssembly(this.GetType()).GetFileVersion());

            if (HostingEnvironment.ApplicationPhysicalPath != null)
            {
                q.Add("apppath_hash", Utilities.Sha256TruncatedBase64(HostingEnvironment.ApplicationPhysicalPath, 6));
            }

            // Add HttpModule class names without prefixing
            SetModules(HttpContext.Current?.ApplicationInstance?.Modules);
            if (HttpModules != null)
            {
                foreach (var name in HttpModules)
                {
                    query.Add("mod", name);
                }
            }
            


            // TODO: check for mismatched assemblies?
        }

        static Tuple<int?, int?> GetIISVerFromRegistry()
        {
            try
            {
                const string subkey = @"SOFTWARE\Microsoft\InetStp\";
                using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                    .OpenSubKey(subkey))
                { 
                    //SetupString
                    return new Tuple<int?, int?>(ndpKey?.GetValue("MajorVersion") as int?, ndpKey?.GetValue("MinorVersion") as int?);
                }
            }
            catch { }
            return null;
        }

        static string GetIISVerStringFromRegistry()
        {
            var pair = GetIISVerFromRegistry();
            if (pair != null && pair.Item1 != null && pair.Item2 != null)
            {
                return string.Format("{0}.{1}", pair.Item1, pair.Item2);
            }
            return null;
        }

        static string Get45PlusFromRegistry()
        {
            try
            {
                const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
                using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
                {
                    // Version
                    if (ndpKey != null && ndpKey.GetValue("Release") != null)
                    {
                        return CheckFor45PlusVersion((int)ndpKey.GetValue("Release"));
                    }
                }
            }
            catch { }
            return null;
        }

        // Checking the version using >= will enable forward compatibility.
        private static string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 460798)
            {
                return "4.7 or later";
            }
            if (releaseKey >= 394802)
            {
                return "4.6.2";
            }
            if (releaseKey >= 394254)
            {
                return "4.6.1";
            }
            if (releaseKey >= 393295)
            {
                return "4.6";
            }
            if ((releaseKey >= 379893))
            {
                return "4.5.2";
            }
            if ((releaseKey >= 378675))
            {
                return "4.5.1";
            }
            if ((releaseKey >= 378389))
            {
                return "4.5";
            }
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
        }

        private static IEnumerable<string> GetHttpModules(HttpModuleCollection modules)
        {
            var sb = new StringBuilder();
            if (modules != null)
            {
                return modules.AllKeys.Select(key => 
                modules.Get(key).GetType().FullName
                .Replace("System.Web.Security", "").Replace("System.Web", ""))
                .ToArray();
            }
            return Enumerable.Empty<string>();
        }
    }
}

