using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ImageResizer.ReleaseBuilder.Classes;
using LitS3;
using System.Net;

namespace ImageResizer.ReleaseBuilder {
    public class Build :Interaction {
        FolderFinder f = new FolderFinder("Core" );
        Devenv d = null;
        FsQuery q = null;
        VersionEditor v = null;
        GitManager g = null;
        NugetManager nuget = null;

        S3Service s3 = new S3Service();
        string bucketName = "resizer-downloads";
        string linkBase = "http://downloads.imageresizing.net/";
        public Build() {
            d = new Devenv(Path.Combine(f.folderPath,"ImageResizer.sln"));
            v = new VersionEditor(Path.Combine(f.folderPath, "SharedAssemblyInfo.cs"));
            g = new GitManager(f.parentPath);
            nuget = new NugetManager(Path.Combine(f.parentPath, "nuget"));
            s3.AccessKeyID = "***REMOVED***";
            s3.SecretAccessKey = "***REMOVED***";
            



            packages.Add(new PackageDescriptor("min", PackMin));
            packages.Add(new PackageDescriptor("full", PackFull));
            //packages.Add(new PackageDescriptor("core", PackCore));
            packages.Add(new PackageDescriptor("standard", PackStandard));
            //packages.Add(new PackageDescriptor("allbinaries", PackAllBinaries));
        }

        public string getReleasePath(string packageBase, string ver,  string kind) {
            return Path.Combine(Path.Combine(f.parentPath, "Releases"), packageBase + ver.Trim('-') + '-' + kind + "-" + DateTime.UtcNow.ToString("MMM-d-yyyy") + ".zip");
        }

        List<PackageDescriptor> packages = new List<PackageDescriptor>();

        [STAThread]
        public void Run() {
            say("Project root: " + f.parentPath);
            nl();
            //The base name for creating zip packags.
            string packageBase = v.get("PackageName"); //    // [assembly: PackageName("Resizer")]

            //a. Ask about hotfix - for hotfixes, we embed warnings and stuff so they don't get used in production.
            bool isHotfix = ask("Is this a hotfix? Press Y to tag the assembiles and packages as such.");
            //Build the hotfix name
            string packageHotfix = isHotfix ? ("-hotfix-" + DateTime.Now.ToString("htt").ToLower()) : "";

            //b. Ask for file version number   [assembly: AssemblyFileVersion("3.0.5.*")]
            string fileVer = change("FileVersion", v.get("AssemblyFileVersion").TrimEnd('.', '*'));
            //c. Ask for assembly version number. AssemblyVersion("3.0.5.*")]
            string assemblyVer = change("AssemblyVersion", v.get("AssemblyVersion").TrimEnd('.', '*'));
            //d: Ask for information version number. (used in zip package names) [assembly: AssemblyInformationalVersion("3-alpha-5")]
            string infoVer = change("InfoVersion", v.get("AssemblyInformationalVersion").TrimEnd('.', '*'));
            //e. Ask for Nuget package version number. New builds need to have a 4th number specified.
            string nugetVer = change("NugetVersion", v.get("NugetVersion").TrimEnd('.', '*'));

            //Get the download server from SharedAssemblyInfo.cs if specified
            string downloadServer = v.get("DownloadServer"); if (downloadServer == null) downloadServer = "http://downloads.imageresizing.net/";


            //f. For each package, specify options: choose 'c' (create and/or overwrite), 'u' (upload), 'p' (make private). 
            //Should inform if the file already exists.
            nl();
            say("For each zip package, specify all operations to perform, then press enter.");
            say("'c' - Create package (overwrite if exists), 'u' (upload to S3), 's' (skip), 'p' (make private)");
            bool isBuilding = false;
            StringBuilder downloadPaths = new StringBuilder();
            foreach (PackageDescriptor desc in packages) {
                desc.Path = getReleasePath(packageBase, infoVer + packageHotfix, desc.Kind);
                if (desc.Exists) say("\n" + Path.GetFileName(desc.Path) + " already exists");
                string opts = "";

                Console.Write(desc.Kind + " (" + opts + "):");
                opts = Console.ReadLine().Trim();
                
                desc.Options = opts;
                if (desc.Build) isBuilding = true;
                if (desc.Upload) {
                    downloadPaths.AppendLine(downloadServer + Path.GetFileName(desc.Path));
                }
            }

            if (downloadPaths.Length > 0){
                say("Once complete, your files will be available at");
                say(downloadPaths.ToString());
                if (ask("Copy these to the clipboard?"))
                    System.Windows.Clipboard.SetText(downloadPaths.ToString());
            }

            //Get all the .nuspec packages on in the /nuget directory.
            IList<NPackageDescriptor> npackages =NPackageDescriptor.GetPackagesIn(Path.Combine(f.parentPath,"nuget"));

            bool isMakingNugetPackage = false;

            if (ask("Create or upload any NuGet packages?")) {

                say("For each nuget package, specify all operations to perform, then press enter. ");
                say("(c (create and overwrite), u (upload to nuget.org)");
                foreach (NPackageDescriptor desc in npackages) {
                    desc.Version = nugetVer;
                    desc.OutputDirectory = Path.Combine(Path.Combine(f.parentPath, "Releases", "nuget-packages"));
                    string opts = "";

                    ConsoleColor original = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (desc.PackageExists) say(Path.GetFileName(desc.PackagePath) + " exists");
                    if (desc.SymbolPackageExists) say(Path.GetFileName(desc.SymbolPackagePath) + " exists");
                    Console.ForegroundColor = original;

                    Console.Write(desc.BaseName + " (" + opts + "):");
                    opts = Console.ReadLine().Trim();

                    desc.Options = opts;
                    if (desc.Build) isMakingNugetPackage = true;

                }
            }
            
            if (!isBuilding && isMakingNugetPackage) {
                isBuilding = ask("You're creating 1 or more NuGet packages. Rebuild software?");
            }

            if (isBuilding) {

                //1 (moved execution to 8a)
                bool cleanAll = ask("Clean All?");

                //2 - Set version numbers (with *, if missing)
                string originalContents = v.Contents; //Save for checking changes.
                v.set("AssemblyFileVersion", v.join(fileVer, "*"));
                v.set("AssemblyVersion", v.join(assemblyVer, "*"));
                v.set("AssemblyInformationalVersion", infoVer);
                v.set("NugetVersion", nugetVer);
                v.set("Commit", "git-commit-guid-here");
                v.Save();
                //Save contents for reverting later
                string fileContents = v.Contents;

                
                //Generate hard revision number for building (so all dlls use the same number)
                short revision = (short)(DateTime.UtcNow.TimeOfDay.Milliseconds % short.MaxValue); //the part under 32767. Can actually go up to, 65534, but what's the point.
                string exactVersion = v.join(fileVer, revision.ToString());
                string fullInfoVer = infoVer + (isHotfix ? ("-temp-hotfix-" + DateTime.Now.ToString("MMM-d-yyyy-htt").ToLower()) : "");
                string tag = "resizer" + v.join(infoVer, revision.ToString()) + (isHotfix ? "-hotfix": "");


                //3 - Prompt to commit and tag
                bool versionsChanged = !fileContents.Equals(originalContents);
                string question = versionsChanged ? "SharedAssemblyInfo.cs was modified. Commit it (and any other changes) to the repository, then hit 'y'."
                    : "Are all changes commited? Hit 'y' to continue. The SHA-1 of HEAD will be embedded in the DLLs.";
                while (!ask(question)) { }

                if (ask("Tag HEAD with '" + tag + "'?"))
                    g.Tag(tag);




                //[assembly: Commit("git-commit-guid-here")]
                //4 - Embed git commit value
                string gitCommit = g.CanExecute ? g.GetHeadHash() : "git-could-not-run-during-build";
                v.set("Commit", gitCommit);

                //4b - change to hard version number for building
                
                v.set("AssemblyFileVersion", exactVersion);
                v.set("AssemblyVersion", exactVersion);
                //Add hotfix suffix for hotfixes
                v.set("AssemblyInformationalVersion", fullInfoVer);
                v.Save();


                //8a Clean projects if specified
                if (cleanAll) {
                    CleanAll();
                }

                //6 - if (c) was specified for any package, build all.
                BuildAll();

                //7 - Revert file to state at commit (remove 'full' version numbers and 'commit' value)
                v.Contents = fileContents;
                v.Save();

                //8b - run cleanup routine
                RemoveUselessFiles();

                //Prepare searchers
                PrepareForPackaging();

                //9 - Pacakge all selected zip configurations
                foreach (PackageDescriptor pd in packages) {
                    if (pd.Skip || !pd.Build) continue;
                    if (pd.Exists && pd.Build) {
                        File.Delete(pd.Path);
                        say("Deleted " + pd.Path);
                    }
                    pd.Builder(pd);
                    //Copy to a 'tozip' version for e-mailing
                    File.Copy(pd.Path, pd.Path.Replace(".zip", ".tozip"), true);
                }


            } 


            //10 - Pacakge all nuget configurations
            foreach (NPackageDescriptor pd in npackages) {
                if (pd.Skip) continue;
                
                if (pd.Build) nuget.Pack(pd);

            }

            //11 - Upload all selected zip configurations
            foreach (PackageDescriptor pd in packages) {
                if (pd.Skip) continue;
                if (pd.Upload) {
                    if (!pd.Exists) {
                        say("Can't upload, file missing: " + pd.Path);
                        continue;
                    }
                    CannedAcl perm =  pd.Private ? CannedAcl.Private : CannedAcl.PublicRead;
                    say("Uploading " + Path.GetFileName(pd.Path) + " to " + bucketName + " with CannedAcl:" + perm.ToString());
                    bool retry = false;
                    do {
                        //Upload
                        try {
                            s3.AddObject(pd.Path, bucketName, Path.GetFileName(pd.Path), "application/zip", perm);
                        } catch (WebException wex) {
                            say("Upload failed: " + wex.Message);
                            retry = ask("Retry upload?");
                        }
                    } while (retry);

                    say("Finished uploading " + Path.GetFileName(pd.Path));
                } 
            }


            //2 - Upload all nuget configurations
            foreach (NPackageDescriptor pd in npackages) {
                if (pd.Skip || !pd.Upload) continue;
                nuget.Push(pd);

            }



            //12 - Generate template for release notes article

            say("Everything is done.");
            
        }

        public void CleanAll(){
            try { System.IO.Directory.Delete(Path.Combine(f.parentPath, "dlls\\trial"), true); } catch { }
            try { System.IO.Directory.Delete(Path.Combine(f.parentPath, "dlls\\release"), true); } catch { }
            try { System.IO.Directory.Delete(Path.Combine(f.parentPath, "dlls\\debug"), true); } catch { }

            d.Run("/Clean Debug");
            d.Run("/Clean Release");
            d.Run("/Clean Trial");

        }

        public void BuildAll() {
            d.Run("/Build Release");//Have to run Release first, since ImageResizerGUI includes the DLLs.
            d.Run("/Build Debug");
            d.Run("/Build Trial");
        }


        public void RemoveUselessFiles() {
            var f = new Futile(Console.Out);
            q = new FsQuery(this.f.parentPath, new string[]{"/.git","^/Releases", "^/Tests/Builder"});


            //delete /Tests/binaries  (*.pdb, *.xml, *.dll)
            //delete /Core/obj folder
            //Deleate all bin,obj,imageacache,uploads, and results folders under /Samples, /Tests, and /Plugins
            f.DelFiles(q.files("^/Tests/binaries/*.(pdb|xml|dll|txt)$"));
            f.DelFiles(q.files("^/(Tests|Plugins|Samples)/*/(bin|obj|imagecache|uploads|results)/*",
                       "^/Core/obj/*"));



            //delete .xml and .pdb files for third-party libs
            f.DelFiles(q.files("^/dlls/*/(Aforge|LitS3|Ionic)*.(pdb|xml)$"));

            //delete Thumbs.db
            //delete */.DS_Store
            f.DelFiles(q.files("/Thumbs.db$",
                                "/.DS_Store$"));
            q = null;
            
        }


        public string[] standardExclusions = new string[]{
                "/.git","^/Releases","^/Legacy","^/Tools/Builder","/thumbs.db$","/.DS_Store$",".suo$",".user$"
            };

        public void PrepareForPackaging() {
            if (q == null) q = new FsQuery(this.f.parentPath, standardExclusions);
            //Don't copy the DotNetZip or Aforge xml file.
            q.exclusions.Add(new Pattern("^/Plugins/Libs/DotNetZip*.xml$"));
            q.exclusions.Add(new Pattern("^/Plugins/Libs/Aforge*.xml$"));
            q.exclusions.Add(new Pattern("^/Tests/Libs/LibDevCassini"));
            q.exclusions.Add(new Pattern("^/Samples/SqlReaderSampleVarChar"));
            q.exclusions.Add(new Pattern("^/Contrib/*/(bin|obj|imagecache|uploads|results)/*"));
            q.exclusions.Add(new Pattern(".config.transform$"));
        }
        public void PackMin(PackageDescriptor desc) {
            // 'min' - /dlls/release/ImageResizer.* - /
            // /*.txt
            using (var p = new Package(desc.Path, this.f.parentPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/readme.txt$"));
                p.Add(q.files("^/Core/license.txt$"), "");
                p.Add(q.files("^/Web.config$"));
            }
        }
        public void PackAllBinaries(PackageDescriptor desc) {
            using (var p = new Package(desc.Path, this.f.parentPath)) {
                p.Add(q.files("^/dlls/release/*.(dll|pdb)$"), "/");
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }
        public void PackFull(PackageDescriptor desc) {
            // 'full'
            using (var p = new Package(desc.Path, this.f.parentPath)) {
                p.Add(q.files("^/(core|plugins|samples|tests)/"));
                p.Add(q.files("^/contrib/azure"));
                p.Add(q.files("^/dlls/(release|debug)"));
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb|xml)$"), "/"); //Make a copy in the root
                
                p.Add(q.files("^/[^/]+.txt$"));
                p.Add(q.files("^/Web.config$"));

                //Make a empty sample app for IIS
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb)$"), "/Samples/BasicIISSite/bin/"); 
                p.Add(q.files("^/Web.config$"),"/Samples/BasicIISSite/");
            }
        }
        public void PackStandard(PackageDescriptor desc) {
            // 'standard'
            List<Pattern> old = q.exclusions;
            q.exclusions = new List<Pattern>(old);
            q.exclusions.Add(new Pattern("^/Core/[^/]+.sln")); //Don't include the regular solution files, they won't load properly.
            using (var p = new Package(desc.Path, this.f.parentPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/dlls/(release|debug)/"));
                p.Add(q.files("^/(core|samples)/"));
                p.Add(q.files("^/[^/]+.txt$"));
                p.Add(q.files("^/Web.config$"));
            }
            q.exclusions = old;
        }


        public void PackCore(PackageDescriptor desc) {
            // 'core' - 
            // /dlls/release/ImageResizer.* -> /
            // /dlls/debug/ImageResizer.* -> /
            // /Core/
            // /Samples/Images
            // /Samples/Core/
            // /*.txt
            using (var p = new Package(desc.Path, this.f.parentPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/Core/",
                              "^/Samples/Images/",
                              "^/Samples/BasicWebApplication/"));
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }







    }
}
