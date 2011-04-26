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

        public void Run() {

            //CleanAll();
            //BuildAll();
            RemoveUselessFiles();
            PrepareForPackaging();
            PackMin();
            PackCore();
            PackStandard();
            PackFull();
        }


        //Devenv Core/ImageResizer.sln /clean Debug
        //Devenv Core/ImageResizer.sln /clean Release
        //Devenv Core/ImageResizer.sln /clean Trial
        public void CleanAll(){
            d.Run("/Clean Debug");
            d.Run("/Clean Release");
            d.Run("/Clean Trial");
        }
        //Devenv SolutionName /build Debug
        //Devenv SolutionName /build Release
        //Devenv SolutionName /build Trial
        public void BuildAll() {
            d.Run("/Build Debug");
            d.Run("/Build Release");
            d.Run("/Build Trial");
        }

        public void RemoveUselessFiles() {
            var f = new Futile(Console.Out);
            q = new FsQuery(this.f.rootPath);

            
            //delete /Tests/binaries  *.pdb, *.xml, *.dll
            //delete /samples/ * /bin/ *.pdb, *.xml, *.dll
            f.DelFiles(q.files("^/Tests/binaries/*.(pdb|xml|dll|txt)$",
                                "^/Samples/*/bin/*.(pdb|xml|dll)$"));

            //delete .xml and .pdb files for third-party libs

            f.DelFiles(q.files("^/dlls/*/(Aforge|LitS3|Ionic)*.(pdb|xml)$"));


            //delete /tests/   /bin and /obj folders
            //delete /samples/ /imagecache
            //delete /core/obj
            //delete Plugins */obj* and */bin
            f.DelFolders(q.folders("^/Tests/*/(bin|obj)$",
                                   "^/Samples/*/(bin|obj|imagecache)$",
                                   "^/Plugins/*/(bin|obj)$",
                                   "^/Core/obj$"));


            //delete Thumbs.db
            //delete */.DS_Store
            q.exclusions = null;
            f.DelFiles(q.files("/Thumbs.db$",
                                "/.DS_Store$"));
            q = new FsQuery(this.f.rootPath);
            
        }
        public string getReleasePath(string kind) {
            return Path.Combine(Path.Combine(f.rootPath, "Releases"),  ver + "-" +  kind + "-" + DateTime.UtcNow.ToString("MMM-dd-yyyy") + ".zip");
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


                }  else Console.WriteLine("No files will be overwritten.");

            }
        }

        public void PrepareForPackaging() {
            if (q != null) q = new FsQuery(this.f.rootPath);
        }
        public void PackMin() {
            // 'min' - /dlls/release/ImageResizer.* - /
            // /*.txt
            using (var p = new Package(getReleasePath("min"), this.f.rootPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }

        public void PackCore() {
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
                              "^/Samples/Core/"));
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }

        public void PackFull() {
            // 'full'
            using (var p = new Package(getReleasePath("full"), this.f.rootPath)) {
                p.Add(q.files("^/(dlls|core|plugins|samples|tests)/"));
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }

        public void PackStandard() {
            // 'standard'
            
            using (var p = new Package(getReleasePath("standard"), this.f.rootPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/dlls/trial/"), "/TrialPlugins/");
                p.Add(q.files("^/(core|samples)/"));
                p.Add(q.files("^/dlls/"));
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }
    }
}
