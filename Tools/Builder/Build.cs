using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BuildTools;
using System.Net;
using System.Collections.Specialized;
using Amazon.S3.Transfer;

namespace ImageResizer.ReleaseBuilder {
    public class Build :Interaction {
        FolderFinder f = new FolderFinder("Core" );
        Devenv d = null;
        FsQuery q = null;
        VersionEditor v = null;
        GitManager g = null;
        NugetManager nuget = null;
        TransferUtility s3 = null;
        
        string bucketName = "resizer-downloads";
        string linkBase = "http://downloads.imageresizing.net/";
        public Build() {
            d = new Devenv(Path.Combine(f.FolderPath,"ImageResizer.sln"));
            v = new VersionEditor(Path.Combine(f.FolderPath, "SharedAssemblyInfo.cs"));
            g = new GitManager(f.ParentPath);
            nuget = new NugetManager(Path.Combine(f.ParentPath, "nuget"));
            
            
            //TODO: nuget and s3 API key need to go.


            packages.Add(new PackageDescriptor("min", PackMin));
            packages.Add(new PackageDescriptor("full", PackFull));
            packages.Add(new PackageDescriptor("standard", PackStandard));
            packages.Add(new PackageDescriptor("allbinaries", PackAllBinaries));
        }

        public string getReleasePath(string packageBase, string ver,  string kind, string hotfix) {
            return Path.Combine(Path.Combine(f.ParentPath, "Releases"), packageBase + ver.Trim('-') + '-' + kind + "-" + (string.IsNullOrWhiteSpace(hotfix) ? "" : (hotfix.Trim('-') +  "-")) + DateTime.UtcNow.ToString("MMM-d-yyyy") + ".zip");
        }

        public NameValueCollection GetNugetVariables() {
            var nvc = new NameValueCollection();
            nvc["author"] = "Nathanael Jones, Imazen";
            nvc["owners"] = "nathanaeljones, imazen";
            nvc["pluginsdlldir"] = @"..\dlls\trial";
            nvc["coredlldir"] = @"..\dlls\release";
            nvc["iconurl"] = "http://imageresizing.net/images/logos/ImageIconPSD100.png";
 

            nvc["plugins"] = "## 30+ plugins available\n\n" + 
                    "Search 'ImageResizer' on nuget.org, or visit imageresizing.net to see 40+ plugins, including WPF, WIC, FreeImage, OpenCV, AForge &amp; Ghostscript (PDF) integrations. " + 
                    "You'll also find  plugins for disk caching, memory caching, Microsoft SQL blob support, Amazon CloudFront, S3, Azure Blob Storage, MongoDB GridFS, automatic whitespace trimming, " +
                    "automatic white balance, octree 8-bit gif/png quantization and transparency dithering, animated gif resizing, watermark &amp; text overlay support, content aware image resizing /" + 
                    " seam carving (based on CAIR), grayscale, sepia, histogram, alpha, contrast, saturation, brightness, hue, Guassian blur, noise removal, and smart sharpen filters, psd editing &amp; " +
                    "rendering, raw (CR2, NEF, DNG, etc.) file exposure, .webp (weppy) support, image batch processing &amp; compression into .zip archives, red eye auto-correction,  face detection, and " + 
                    "secure (signed!) remote HTTP image processing. Most datastore plugins support the Virtual Path Provider system, and can be used for non-image files as well.\n\n";
                    

            return nvc;
        }

        List<PackageDescriptor> packages = new List<PackageDescriptor>();

        [STAThread]
        public void Run() {
            MakeConsoleNicer();
            //PrepareForPackaging();
            //using (RestorePoint rp = new RestorePoint(q.files(new Pattern("^/Samples/*/*.(cs|vb)proj$")))) {

            //    //Replace all project references temporarily
            //    foreach (string pf in q.files(new Pattern("^/Samples/[^/]+/*.(cs|vb)proj$"))) {
            //        new ProjectFileEditor(pf).ReplaceAllProjectReferencesWithDllReferences("..\\..\\dlls\\release");
            //    }

            //}

            say("Project root: " + f.ParentPath);
            nl();
            //The base name for creating zip packags.
            string packageBase = v.get("PackageName"); //    // [assembly: PackageName("Resizer")]


            //List the file version number   [assembly: AssemblyFileVersion("3.0.5.*")]
            string fileVer = list("FileVersion", v.get("AssemblyFileVersion").TrimEnd('.', '*'));
            //List the assembly version number. AssemblyVersion("3.0.5.*")]
            string assemblyVer = list("AssemblyVersion", v.get("AssemblyVersion").TrimEnd('.', '*'));
            //List the information version number. (used in zip package names) [assembly: AssemblyInformationalVersion("3-alpha-5")]
            string infoVer = list("InfoVersion", v.get("AssemblyInformationalVersion").TrimEnd('.', '*'));
            //List the Nuget package version number. New builds need to have a 4th number specified.
            string nugetVer = list("NugetVersion", v.get("NugetVersion").TrimEnd('.', '*'));

            //a. Ask if version numbers need to be modified
            if (ask("Change version numbers?")) {
                //b. Ask for file version number   [assembly: AssemblyFileVersion("3.0.5.*")]
                fileVer = change("FileVersion", v.get("AssemblyFileVersion").TrimEnd('.', '*'));
                //c. Ask for assembly version number. AssemblyVersion("3.0.5.*")]
                assemblyVer = change("AssemblyVersion", v.get("AssemblyVersion").TrimEnd('.', '*'));
                //d: Ask for information version number. (used in zip package names) [assembly: AssemblyInformationalVersion("3-alpha-5")]
                infoVer = change("InfoVersion", v.get("AssemblyInformationalVersion").TrimEnd('.', '*'));
                //e. Ask for Nuget package version number. New builds need to have a 4th number specified.
                nugetVer = change("NugetVersion", v.get("NugetVersion").TrimEnd('.', '*'));
            }

            //b. Ask about hotfix - for hotfixes, we embed warnings and stuff so they don't get used in production.
            bool isHotfix = ask("Is this a hotfix? Press Y to tag the assembiles and packages as such.");
            //Build the hotfix name
            string packageHotfix = isHotfix ? ("-hotfix-" + DateTime.Now.ToString("htt").ToLower()) : "";


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
                desc.Path = getReleasePath(packageBase, infoVer, desc.Kind, packageHotfix);
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
            IList<NPackageDescriptor> npackages =NPackageDescriptor.GetPackagesIn(Path.Combine(f.ParentPath,"nuget"));

            bool isMakingNugetPackage = false;

            if (ask("Create or upload NuGet packages?")) {

                foreach (NPackageDescriptor desc in npackages) {
                    desc.VariableSubstitutions = GetNugetVariables();
                    desc.VariableSubstitutions["version"] = nugetVer;
                    desc.Version = nugetVer;
                    desc.OutputDirectory = Path.Combine(Path.Combine(f.ParentPath, "Releases", "nuget-packages"));

                    if (!Directory.Exists(desc.OutputDirectory)) Directory.CreateDirectory(desc.OutputDirectory);

                    say(Path.GetFileName(desc.PackagePath) + (desc.PackageExists ?  " exists" : " not found"), desc.PackageExists ? ConsoleColor.Green : ConsoleColor.Gray);
                    say(Path.GetFileName(desc.SymbolPackagePath) + (desc.SymbolPackageExists ? " exists" : " not found"), desc.SymbolPackageExists ? ConsoleColor.Green : (desc.PackageExists ? ConsoleColor.Red : ConsoleColor.Gray));

                }


                say("What should we do with these packages? Enter multiple options like 'ou' ");
                say("r (create missing packages), c (overwrite all packages), u (upload all packages to nuget.org), i (enter interactive mode - choose per package), s (skip)");

                string selection = Console.ReadLine().Trim().ToLowerInvariant();
                bool interactive = selection.IndexOf('i') > -1;
                if (interactive) selection = selection.Replace("i","");

                //Set the default for every package
                foreach (NPackageDescriptor desc in npackages) desc.Options = selection;

                //Let the user pick per package
                if (interactive)
                {
                    foreach (NPackageDescriptor desc in npackages)
                    {
                        Console.Write(desc.BaseName + " (" + desc.Options + "):");
                        desc.Options = Console.ReadLine().Trim().ToLowerInvariant();
                    }
                }

                isMakingNugetPackage = npackages.Any(desc => desc.Build);

            }

            var cs = new CredentialStore();
            if (downloadPaths.Length > 0) {
                cs.Need("S3ID", "Amazon S3 AccessKey ID");
                cs.Need("S3KEY", "Amazon S3 SecretAccessKey");
            }
            if (isMakingNugetPackage) cs.Need("NugetKey", "NuGet API Key");

            cs.AcquireCredentials();

            nuget.apiKey = cs.Get("NugetKey",null);

            string s3ID = cs.Get("S3ID",null);
            string s3Key = cs.Get("S3KEY", null);

            var s3config = new Amazon.S3.AmazonS3Config();
            s3config.Timeout = TimeSpan.FromHours(12);
            s3config.RegionEndpoint = Amazon.RegionEndpoint.USEast1;
            var s3client = new Amazon.S3.AmazonS3Client(s3ID, s3Key,s3config);
            s3 = new TransferUtility(s3client);


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


                //Prepare searchersq
                PrepareForPackaging();

                bool success = false;
                //Allows use to temporarily edit all the sample project files
                using (RestorePoint rp = new RestorePoint(q.files(new Pattern("^/Plugins/*/*.(cs|vb)proj$"), new Pattern("^/Contrib/*/*.(cs|vb)proj$")))) {

                    //Replace all project references temporarily
                    foreach (string pf in rp.Paths) {
                        new ProjectFileEditor(pf).RemoveStrongNameRefs();
                    }

                    //8a Clean projects if specified
                    if (cleanAll) {
                        CleanAll();
                    }

                    //6 - if (c) was specified for any package, build all.
                    success = BuildAll(true); //isMakingNugetPackage);
                }

                //7 - Revert file to state at commit (remove 'full' version numbers and 'commit' value)
                v.Contents = fileContents;
                q.Rescan(); //Rescan filesystem to prevent errors building the archive (since we delete stuff in CleanAll())
                v.Save();

                if (!success) return; //If the build didn't go ok, pause and exit

                //8b - run cleanup routine
                RemoveUselessFiles();


                //Allows use to temporarily edit all the sample project files
                using (RestorePoint rp = new RestorePoint(q.files(new Pattern("^/Samples/*/*.(cs|vb)proj$")))) {

                    //Replace all project references temporarily
                    foreach (string pf in q.files(new Pattern("^/Samples/[^/]+/*.(cs|vb)proj$"))) {
                        new ProjectFileEditor(pf).ReplaceAllProjectReferencesWithDllReferences("..\\..\\dlls\\release").RemoveStrongNameRefs();
                    }


                    //9 - Pacakge all selected zip configurations
                    foreach (PackageDescriptor pd in packages) {
                        if (pd.Skip || !pd.Build) continue;
                        if (pd.Exists && pd.Build) {
                            File.Delete(pd.Path);
                            say("Deleted " + pd.Path);
                        }
                        pd.Builder(pd);
                        //Copy to a 'tozip' version for e-mailing
                        //File.Copy(pd.Path, pd.Path.Replace(".zip", ".tozip"), true);
                    }
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
                    var request = new TransferUtilityUploadRequest();
                    request.CannedACL = pd.Private ? Amazon.S3.S3CannedACL.Private : Amazon.S3.S3CannedACL.PublicRead;
                    request.BucketName = bucketName;
                    request.ContentType = "application/zip";
                    request.Key = Path.GetFileName(pd.Path);
                    request.FilePath = pd.Path;


                    say("Uploading " + Path.GetFileName(pd.Path) + " to " + bucketName + " with CannedAcl:" + request.CannedACL.ToString());
                    bool retry = false;
                    do {
                        //Upload
                        try {
                            
                            s3.Upload(request);
                        } catch (Exception ex) {
                            say("Upload failed: " + ex.Message);
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
            try { System.IO.Directory.Delete(Path.Combine(f.ParentPath, "dlls\\trial"), true); } catch { }
            try { System.IO.Directory.Delete(Path.Combine(f.ParentPath, "dlls\\release"), true); } catch { }
            try { System.IO.Directory.Delete(Path.Combine(f.ParentPath, "dlls\\debug"), true); } catch { }

            d.Run("/Clean Debug");
            d.Run("/Clean Release");
            d.Run("/Clean Trial");

        }

        public bool BuildAll(bool buildDebug) {
            int result = d.Run("/Build Release") + //Have to run Release first, since ImageResizerGUI includes the DLLs.
            d.Run("/Build Trial");
            if (buildDebug) result += d.Run("/Build Debug");

            if (result > 0 && !ask("There may have been build errors. Continue?")) return false;
            return true;
        }


        public void RemoveUselessFiles() {
            var f = new Futile(Console.Out);
            var q = new FsQuery(this.f.ParentPath, new string[]{"/.git","^/Releases", "^/Tests/Builder"});


            //delete /Tests/binaries  (*.pdb, *.xml, *.dll)
            //delete /Core/obj folder
            //Deleate all bin,obj,imageacache,uploads, and results folders under /Samples, /Tests, and /Plugins
            f.DelFiles(q.files("^/(Tests|Plugins|Samples)/*/(bin|obj|imagecache|uploads|results)/*",
                       "^/Core/obj/*","^/Core.Mvc/obj/*"));


            f.DelFiles(q.files("^/Samples/MvcSample/App_Data/*"));

            //delete .xml and .pdb files for third-party libs
            f.DelFiles(q.files("^/dlls/*/(Aforge|LitS3|Ionic)*.(pdb|xml)$"));

            //delete Thumbs.db
            //delete */.DS_Store
            f.DelFiles(q.files("/Thumbs.db$",
                                "/.DS_Store$"));
            q = null;
            
        }


        public string[] standardExclusions = new string[]{
                "/.git","^/Releases","/Hidden/","^/Legacy","^/Tools/(Builder|BuildTools|docu)", "^/submodules/docu",
                "^/Samples/Images/(extra|private)/","/Thumbs.db$","/.DS_Store$",".suo$",".cache$",".user$", "/._","/~$", 
                "^/Samples/MvcSample/App_Data/"

            };

        public void PrepareForPackaging() {
            if (q == null) q = new FsQuery(this.f.ParentPath, standardExclusions);
            //Don't copy XML or PDB files for the following libs:
            q.exclusions.Add(new Pattern("/(Newtonsoft.Json|DotNetZip|Aforge|LitS3|Ionic|NLog|MongoDB|Microsoft.|AWSSDK)*.(xml|pdb)$"));
            //Don't copy XML for these (but do keep pdb)
            q.exclusions.Add(new Pattern("/(OpenCvSharp|FreeImageNet)*.xml$"));
            //Exclude dependencies handled by NDP
            q.exclusions.Add(new Pattern("/(FreeImage|gsdll32|gsdll64).dll$")); 
            
            //Exclude infrequently used but easily buildable stuff
            q.exclusions.Add(new Pattern("/ImageResizerGUI.exe$"));
            
            //Exclude resharper junk
            q.exclusions.Add(new Pattern("_ReSharper"));

            //Exclude temorary files
            q.exclusions.Add(new Pattern("^/Contrib/*/(bin|obj|imagecache|uploads|results)/*"));
            q.exclusions.Add(new Pattern("^/(Tests|Plugins|Samples)/*/(bin|obj|imagecache|uploads|hidden|results)/"));
            q.exclusions.Add(new Pattern("^/Core(.Mvc)?/obj/"));
            q.exclusions.Add(new Pattern("^/Tests/binaries"));

            //Exclude stuff that is not used or generally useful
            q.exclusions.Add(new Pattern("^/Tests/LibDevCassini"));
            q.exclusions.Add(new Pattern("^/Tests/ComparisonBenchmark/Images"));
            q.exclusions.Add(new Pattern("^/Samples/SqlReaderSampleVarChar"));
            q.exclusions.Add(new Pattern(".config.transform$"));
            q.exclusions.Add(new Pattern("^/Plugins/Libs/FreeImage/Examples/")); //Exclude examples folder
            q.exclusions.Add(new Pattern("^/Plugins/Libs/FreeImage/Wrapper/(Delphi|VB6|FreeImagePlus)")); //Exclude everything except the FreeImage.NET folder
            q.exclusions.Add(new Pattern("^/Plugins/Libs/FreeImage/Wrapper/FreeImage.NET/cs/[^L]*/")); //Exclude everything except the library folder
            
        }
        public void PackMin(PackageDescriptor desc) {
            // 'min' - /dlls/release/ImageResizer.* - /
            // /*.txt
            using (var p = new Package(desc.Path, this.f.ParentPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$"), "/", "dlls/release");
                p.Add(q.files("^/readme.txt$"));
                p.Add(q.files("^/Core/license.txt$"), "");
                p.Add(q.files("^/Web.config$"));
            }
        }
        public void PackAllBinaries(PackageDescriptor desc) {
            using (var p = new Package(desc.Path, this.f.ParentPath)) {
                p.Add(q.files("^/dlls/release/*.(dll|pdb)$"), "/", "dlls/release");
                p.Add(q.files("^/[^/]+.txt$"));
            }
        }
        public void PackFull(PackageDescriptor desc) {
            // 'full'
            using (var p = new Package(desc.Path, this.f.ParentPath)) {
                p.Add(q.files("^/(core|contrib|core.mvc|plugins|samples|tests|studiojs)/"));
                p.Add(q.files("^/tools/COMInstaller"));
                p.Add(q.files("^/dlls/(debug|release)"));
                p.Add(q.files("^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$"), "/"); //Make a copy in the root
                p.Add(q.files("^/submodules/studiojs"), "/StudioJS"); //Copy submodules/studiojs -> /StudioJS
                p.Add(q.files("^/submodules/(lightresize|libwebp-net)")); 
                p.Add(q.files("^/[^/]+.txt$"));
                p.Add(q.files("^/Web.config$"));

                //Make a empty sample app for IIS
                p.Add(q.files("^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb)$"), "/Samples/BasicIISSite/bin/");
                p.Add(q.files("^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb)$"), "/Samples/SampleAspSite/bin/");
                p.Add(q.files("^/dlls/release/ImageResizer.Plugins.RemoteReader.(dll|pdb)$"), "/Samples/SampleAspSite/bin/"); 
                p.Add(q.files("^/Web.config$"),"/Samples/BasicIISSite/");
            }
        }
        public void PackStandard(PackageDescriptor desc) {
            // 'standard'
            List<Pattern> old = q.exclusions;
            q.exclusions = new List<Pattern>(old);
            q.exclusions.Add(new Pattern("^/Core/[^/]+.sln")); //Don't include the regular solution files, they won't load properly.
            using (var p = new Package(desc.Path, this.f.ParentPath)) {
                p.Add(q.files("^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/dlls/(debug|release)/"));
                p.Add(q.files("^/submodules/studiojs"), "/StudioJS"); //Copy submodules/studiojs -> /StudioJS
                p.Add(q.files("^/(core|samples)/"));
                p.Add(q.files("^/[^/]+.txt$"));
                p.Add(q.files("^/Web.config$"));
            }
            q.exclusions = old;
        }







    }
}
