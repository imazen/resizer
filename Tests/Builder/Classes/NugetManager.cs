using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ImageResizer.ReleaseBuilder.Classes {
    public class NugetManager {

        public NugetManager(string nugetFolder) {
            this.nugetDir = nugetFolder;
            nugetExe = Path.Combine(nugetDir, "nuget.exe");
            if (!File.Exists(nugetExe)) Console.WriteLine("Failed to find nuget.exe in " +  nugetDir);

        }
        public string nugetExe = null;
        public string nugetDir = null;

        public void Pack(NPackageDescriptor desc) {

            //11 - Pack and upload nuget packages
            //Pack symbols first, then rename them.
            if (desc.SymbolSpecPath != null) {
                pack(desc, desc.SymbolSpecPath, desc.Version);
                //nuget.exe has a bug - the symbol package is build using the Name.nupkg instead of Name.symbols.nupkg format.
                //So we copy it, then overwrite it with the main package.
                if (File.Exists(desc.PackagePath)) File.Copy(desc.PackagePath, desc.SymbolPackagePath, true);
            }
            if (desc.SpecPath != null) pack(desc,desc.SpecPath, desc.Version);
           
        }

        public void Push(NPackageDescriptor desc) {
            if (desc.SpecPath != null) Console.Write(exec("push \"" + desc.PackagePath + "\""));
            if (desc.SymbolSpecPath != null) Console.Write(exec("push \"" + desc.SymbolPackagePath + "\""));
        }

        public void pack(NPackageDescriptor desc, string spec, string version) {
            string oldText = File.ReadAllText(spec);
            string newText = oldText.Replace("$version$", version);
            File.WriteAllText(spec, newText, Encoding.UTF8); //Set version value
            string arguments = "pack " + Path.GetFileName(spec) + " -Version " + version;
            arguments += " -OutputDirectory " + desc.OutputDirectory;
            Console.Write(exec(arguments));
            File.WriteAllText(spec, oldText, Encoding.UTF8); //restore file
        }

        public string exec(string command) {
            var psi = new ProcessStartInfo(nugetExe);
            psi.Arguments = ' ' + command;
            psi.WorkingDirectory = nugetDir;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            psi.RedirectStandardError = true;
            Console.WriteLine("Executing " + psi.FileName + " " + psi.Arguments);
            var p = Process.Start(psi);

            p.WaitForExit();
            return p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        }
    }
}
