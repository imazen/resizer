using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ImageResizer.ReleaseBuilder {
    public class Devenv {

        protected string solutionPath = null;
        public Devenv(string solutionPath){
            this.solutionPath = solutionPath;
        }

        /// <summary>
        /// Returns the location of devenv.exe
        /// </summary>
        public static string DevenvPath{
            get{
                string toolsDir = Environment.GetEnvironmentVariable("VS100COMNTOOLS").TrimEnd('\\','/');
                return Path.Combine(Path.Combine(Path.GetDirectoryName(toolsDir),"IDE"), "devenv.exe");

            }
        }

        public int Run(string args){
            var psi = new ProcessStartInfo(DevenvPath);
            psi.Arguments = '"' + solutionPath + "\" " + args;
            psi.WorkingDirectory = Path.GetDirectoryName(solutionPath);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            Console.WriteLine("Executing " + psi.FileName + " " + psi.Arguments);
            var p = Process.Start(psi);
            p.WaitForExit();
            ConsoleColor original = Console.ForegroundColor;
            Console.WriteLine(p.StandardOutput.ReadToEnd());
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(p.StandardError.ReadToEnd());
            Console.ForegroundColor = original;
            return p.ExitCode;
        }

 //       System.Diagnostics.ProcessStartInfo psi =
 //  new System.Diagnostics.ProcessStartInfo(@"C:\listfiles.bat");
 //psi.RedirectStandardOutput = true;
 //psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
 //psi.UseShellExecute = false;
 //System.Diagnostics.Process listFiles;
 //listFiles = System.Diagnostics.Process.Start(psi);
 //System.IO.StreamReader myOutput = listFiles.StandardOutput;
 //listFiles.WaitForExit(2000);
 //if (listFiles.HasExited)
 // {
 // string output = myOutput.ReadToEnd();
 // this.processResults.Text = output;

 //       VS100COMNTOOLS

    }
}
