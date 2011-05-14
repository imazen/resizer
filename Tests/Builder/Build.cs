using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ImageResizer.ReleaseBuilder.Classes;

namespace ImageResizer.ReleaseBuilder {
    public class Build :Interaction {
        FolderFinder f = new FolderFinder("Core" );
        Devenv d = null;
        FsQuery q = null;
        string ver;

        public Build(string version) {
            ver = version;
            d = new Devenv(Path.Combine(f.folderPath,"ImageResizer.sln"));
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
            q = new FsQuery(this.f.parentPath, new string[]{"/.git","^/Releases", "^/Tests/Builder"});


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
            f.DelFiles(q.files("/Thumbs.db$",
                                "/.DS_Store$"));
            q = null;
            
        }


        public string[] standardExclusions = new string[]{
                "/.git","^/Releases","^/Legacy","^/Tests/Builder","/thumbs.db$","/.DS_Store$",".suo$",".user$"
            };

        public void PrepareForPackaging() {
            if (q != null) q = new FsQuery(this.f.parentPath, standardExclusions);
            //Don't copy the DotNetZip xml file.
            q.exclusions.Add(new Pattern("^/Plugins/Libs/DotNetZip*.xml$"));
            q.exclusions.Add(new Pattern("^/Tests/Libs/LibDevCassini"));

        }
        public void PackMin() {
            if (!ask("Create 'min' package?")) return;
            // 'min' - /dlls/release/ImageResizer.* - /
            // /*.txt
            using (var p = new Package(getReleasePath("min"), this.f.parentPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }

        public void PackFull() {
            if (!ask("Create 'full' package?")) return;
            // 'full'
            using (var p = new Package(getReleasePath("full"), this.f.parentPath)) {
                p.Add(q.files("^/(core|plugins|samples|tests)/"));
                p.Add(q.files("^/dlls/(release|trial)"));
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
            using (var p = new Package(getReleasePath("core"), this.f.parentPath)) {
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
            
            using (var p = new Package(getReleasePath("standard"), this.f.parentPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/dlls/trial/"), "/TrialPlugins/");
                p.Add(q.files("^/(core|samples)/"));
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }




        public string getReleasePath(string kind) {
            return Path.Combine(Path.Combine(f.parentPath, "Releases"), ver + "-" + kind + "-" + DateTime.UtcNow.ToString("MMM-dd-yyyy") + ".zip");
        }

        public void CheckPackagesExist() {
            string[] paths = new string[] { getReleasePath("min"), getReleasePath("core"), getReleasePath("full"), getReleasePath("standard") };
            
            PromptDeleteBatch("The following packages already exist. Delete them so new ones can be created?", paths);


        }
    }
}
