using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace COMInstaller {
    /// <summary>
    /// Handles execution of regasm 32 and 64-bit versions.
    /// </summary>
    public class Regasm {

        public static string regasm32;
        public static string regasm64;

        public string warning="RegAsm : warning RA0000 : Registering an unsigned assembly with /codebase " + 
            "can cause your assembly to interfere with other applications that may be installed on" +
            " the same computer. The /codebase switch is intended to be used only with signed" + 
            " assemblies. Please give your assembly a strong name and re-register it.";

         static Regasm() {
            string dotnet = Path.Combine(Path.GetDirectoryName(System.Environment.SystemDirectory), "Microsoft.NET");
            regasm32 = Path.Combine(Path.Combine(Path.Combine(dotnet, "Framework"), "v2.0.50727"), "regasm.exe");
            regasm64 = Path.Combine(Path.Combine(Path.Combine(dotnet, "Framework64"), "v2.0.50727"), "regasm.exe");
            if (!File.Exists(regasm32)) regasm32 = null;
            if (!File.Exists(regasm64)) regasm64 = null;
            if (regasm32 == null && regasm64 == null) throw new FileNotFoundException("Could not find regasm.exe. Is the .NET 2.0 framework installed?");
        }
            
        public Regasm() {

        }
        /// <summary>
        /// Registers the specified assembly with both 32 and 64-bit versions (if available). 
        /// </summary>
        /// <param name="path">The full path to the DLL</param>
        /// <param name="codebase">True to embed the path of the DLL in the registry.</param>
        /// <param name="unregister">True to unregister the dll</param>
        /// <returns></returns>
        public string Register(string path, bool codebase, bool unregister) {
            string args = "\"" + path + "\" /nologo" + (codebase ? " /codebase" : "") + (unregister ? " /u" : "");
            string result = "";
            if (regasm32 != null) result += exec(regasm32, args, Path.GetDirectoryName(path));
            if (regasm64 != null) result += exec(regasm64, args, Path.GetDirectoryName(path));
            return result;
        }

        protected string exec(string exepath, string command, string workingDir) {
            ProcessStartInfo psi = new ProcessStartInfo(exepath);
            psi.Arguments = ' ' + command.TrimStart(' ');
            psi.WorkingDirectory = workingDir;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            Process p = Process.Start(psi);
            p.WaitForExit();
            //Remove the codebase warning
            return (p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd()).Replace(warning,"");
        }

    }
}
