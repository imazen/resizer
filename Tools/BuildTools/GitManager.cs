using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace BuildTools {
    public class GitManager {

        string gitExe = null;
        string repoRoot = null;
        public GitManager(string repoRoot) {
            gitExe = FindGit();
            this.repoRoot = repoRoot;
        }
        /// <summary>
        /// Searchs all ProgramFiles locations. Looks for \Git\bin\git.exe
        /// </summary>
        /// <returns></returns>
        public string FindGit() {
            //C:\Program Files\Git\bin\git.exe
            //"ProgramFiles", "ProgramFiles(x86)","ProgramW6432"
            string[] vars = new string[] { "ProgramFiles", "ProgramFiles(x86)", "ProgramW6432" };
            foreach (string v in vars) {
                string[] subfolders = new string[] { @"\Git\bin", @"\SmartGitHg\git\bin",@"\SmartGitHg 4.6\git\bin", @"\SmartGitHg 4.5\git\bin",  @"\SmartGitHg 4\git\bin"};

                foreach (string subfolder in subfolders)
                {
                    string progFiles = Environment.GetEnvironmentVariable(v).TrimEnd('\\', '/');
                    string path = progFiles + subfolder + @"\git.exe".Replace('\\', System.IO.Path.DirectorySeparatorChar);
                    if (File.Exists(path)) return path;
                }
            }
            Console.WriteLine("Failed to find git.exe.");
            return null;

        }

        public bool CanExecute { get { return repoRoot != null && gitExe != null; } }

        public string GetHeadHash() {
            return exec("log -1 --pretty=format:%h");
        }

        public string Tag(string name) {
            return exec("tag " + name);
        }


        public string exec(string command) {
            var psi = new ProcessStartInfo(gitExe);
            psi.Arguments = ' ' + command;
            psi.WorkingDirectory = repoRoot;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            
            psi.RedirectStandardError = true;
            Console.WriteLine("Executing " + psi.FileName + " " + psi.Arguments);
            var p = Process.Start(psi);
            
            p.WaitForExit();
            return p.StandardOutput.ReadToEnd();
        }
    }
}
