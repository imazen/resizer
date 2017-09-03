using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security;

namespace ImageResizer.Configuration.Performance
{
    class ProcessInfo
    {
        public static Guid ProcessGuid { get; } = Guid.NewGuid();

        public bool Process64Bit { get; } = Environment.Is64BitProcess;

        
        public ProcessInfo() { }

        public void Add(IInfoAccumulator query)
        {
            var q = query.WithPrefix("proc_");
            q.Add("64", Process64Bit);
            q.Add("guid", ProcessGuid);
           
            q.Add("working_set_mb", Environment.WorkingSet / 1000000);
            q.Add("git_commit", Assembly.GetAssembly(this.GetType()).GetShortCommit());
            q.Add("info_version", Assembly.GetAssembly(this.GetType()).GetInformationalVersion());
            q.Add("file_version", Assembly.GetAssembly(this.GetType()).GetFileVersion());
            
        }

        public static string MainModuleFileName()
        {
            try
            {
                return System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            }
            catch (Win32Exception)
            {
                return "(process filename cannot be determined, access denied)";
            }
        }
        
    }
}

