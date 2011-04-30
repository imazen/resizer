using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ImageResizer.ReleaseBuilder {
    public class Build {
        SolutionFinder f = new SolutionFinder();
        Devenv d = null;
        FsQuery q = null;
        string ver;
        public Build(string version) {
            ver = version;
            d = new Devenv(f.solutionPath);
        }

        public bool ask(string question) {
            Console.WriteLine(question);
            bool yes =  Console.ReadKey(false).KeyChar.ToString().ToLower().Equals("y");
            Console.WriteLine();
            return yes;
        }

        public void Run() {

            CleanAll();
            BuildAll();
            RemoveUselessFiles();
            PrepareForPackaging();
            CheckPackagesExist();
            PackMin();
            PackFull();
            PackCore();
            PackStandard();
        }

        public void CleanAll(){
            if (!ask("Clean all?")) return;
            d.Run("/Clean Debug");
            d.Run("/Clean Release");
            d.Run("/Clean Trial");
        }

        public void BuildAll() {
            if (!ask("Build all?")) return;
            d.Run("/Build Debug");
            d.Run("/Build Release");
            d.Run("/Build Trial");
        }

        public void RemoveUselessFiles() {
            var f = new Futile(Console.Out);
            q = new FsQuery(this.f.rootPath);

            
            //delete /Tests/binaries  (*.pdb, *.xml, *.dll)
            //delete all bin and obj folders under /Tests and /Plugins
            //delete /Core/obj folder
            //Deleate all bin,obj,imageacache,uploads, and results folders under /Samples
            f.DelFiles(q.files("^/Tests/binaries/*.(pdb|xml|dll|txt)$"));
            f.DelFolders(q.folders("^/(Tests|Plugins)/*/(bin|obj)$",
                       "^/Samples/*/(bin|obj|imagecache|uploads|results)$",
                       "^/Core/obj$"));



            //delete .xml and .pdb files for third-party libs
            f.DelFiles(q.files("^/dlls/*/(Aforge|LitS3|Ionic)*.(pdb|xml)$"));

            //delete Thumbs.db
            //delete */.DS_Store
            q.exclusions = null;
            f.DelFiles(q.files("/Thumbs.db$",
                                "/.DS_Store$"));
            q = new FsQuery(this.f.rootPath);
            
        }


        public void PrepareForPackaging() {
            if (q != null) q = new FsQuery(this.f.rootPath);
            //Don't copy the DotNetZip xml file.
            q.exclusions.Add(new Pattern("^/Plugins/Libs/DotNetZip*.xml$"));
            q.exclusions.Add(new Pattern("^/Tests/Libs/LibDevCassini"));

        }
        public void PackMin() {
            if (!ask("Create 'min' package?")) return;
            // 'min' - /dlls/release/ImageResizer.* - /
            // /*.txt
            using (var p = new Package(getReleasePath("min"), this.f.rootPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }

        public void PackFull() {
            if (!ask("Create 'full' package?")) return;
            // 'full'
            using (var p = new Package(getReleasePath("full"), this.f.rootPath)) {
                p.Add(q.files("^/(dlls|core|plugins|samples|tests)/"));
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }

        public void PackCore() {
            if (!ask("Create 'core' package?")) return;
            // 'core' - 
            // /dlls/release/ImageResizer.* -> /
            // /dlls/debug/ImageResizer.* -> /
            // /Core/
            // /Samples/Images
            // /Samples/Core/
            // /*.txt
            using (var p = new Package(getReleasePath("core"), this.f.rootPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/Core/",
                              "^/Samples/Images/",
                              "^/Samples/BasicWebApplication/"));
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }



        public void PackStandard() {
            if (!ask("Create 'standard' package?")) return;
            // 'standard'
            
            using (var p = new Package(getReleasePath("standard"), this.f.rootPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/dlls/trial/"), "/TrialPlugins/");
                p.Add(q.files("^/(core|samples)/"));
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }




        public string getReleasePath(string kind) {
            return Path.Combine(Path.Combine(f.rootPath, "Releases"), ver + "-" + kind + "-" + DateTime.UtcNow.ToString("MMM-dd-yyyy") + ".zip");
        }

        public void CheckPackagesExist() {
            string[] paths = new string[] { getReleasePath("min"), getReleasePath("core"), getReleasePath("full"), getReleasePath("standard") };
            List<string> exist = new List<string>();
            foreach (string s in paths)
                if (File.Exists(s)) exist.Add(s);

            if (exist.Count > 0) {
                Console.WriteLine("The following packages already exist. Overwrite them?");
                foreach (string s in exist) Console.WriteLine(s);

                if (Console.ReadKey(false).KeyChar.ToString().ToLower().Equals("y")) {
                    foreach (string s in exist) {

                        File.Delete(s);
                        Console.WriteLine("Deleted " + s);
                    }


                } else Console.WriteLine("No files will be overwritten.");

            }
        }
    }
}
