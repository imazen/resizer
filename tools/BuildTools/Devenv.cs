using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace BuildTools {
    /// <summary>
    /// a wrapper class for executing commands against a visual studio solution
    /// </summary>
    public class Devenv {

        protected string solutionPath = null;
        /// <summary>
        /// Creates a wrapper class for executing commands against a visual studio solution using devenv.exe (VS 2010
        /// </summary>
        /// <param name="solutionPath"></param>
        public Devenv(string solutionPath){
            this.solutionPath = solutionPath;
        }

        /// <summary>
        /// Returns the location of devenv.exe
        /// </summary>
        public static string DevenvPath{
            get{
                //We're assuming that the latest visual studio (even partially installed) is fully installed. This can be a faulty assumption.
                string toolsDir = (Environment.GetEnvironmentVariable("VS120COMNTOOLS") ?? Environment.GetEnvironmentVariable("VS110COMNTOOLS") ?? Environment.GetEnvironmentVariable("VS100COMNTOOLS")).TrimEnd('\\', '/');
                return Path.Combine(Path.Combine(Path.GetDirectoryName(toolsDir),"IDE"), "devenv.exe");

            }
        }
        /// <summary>
        /// Executes the specified arguments against the solution specified in the class constructor, using VS2010's devenv.exe interface.
        /// Outputs stdout and stderr to the Console, with stderr in red text.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public int Run(string args, string solutionPath = null){
            if (solutionPath == null) solutionPath = this.solutionPath;
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
            if (p.ExitCode != 0) Console.WriteLine("Visual Studio may have encountered errors during the build.");
            Console.ForegroundColor = original;
            return p.ExitCode;
        }


    }
}
