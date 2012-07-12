using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BuildTools;
using LitS3;
using System.Net;
using System.Collections.Specialized;

namespace ImageResizer.ReleaseBuilder {
    public class Build :Interaction {
        FolderFinder f = new FolderFinder("Core" );
        Devenv d = null;
        Devenv extras = null;
        FsQuery q = null;
        VersionEditor v = null;
        GitManager g = null;
        NugetManager nuget = null;

        S3Service s3 = new S3Service();
        string bucketName = "resizer-downloads";
        string linkBase = "http://downloads.imageresizing.net/";
        public Build() {
            d = new Devenv(Path.Combine(f.FolderPath,"ImageResizer.sln"));
            extras = new Devenv(Path.Combine(f.FolderPath, "Other-Plugins-With-External-Dependencies.sln"));
            v = new VersionEditor(Path.Combine(f.FolderPath, "SharedAssemblyInfo.cs"));
            g = new GitManager(f.ParentPath);
            nuget = new NugetManager(Path.Combine(f.ParentPath, "nuget"));
            s3.AccessKeyID = "***REMOVED***";
            s3.SecretAccessKey = "***REMOVED***";
            
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
            nvc["author"] = "Nathanael Jones";
            nvc["pluginsdlldir"] = @"..\dlls\trial";
            nvc["coredlldir"] = @"..\dlls\release";
            nvc["iconurl"] = "http://imageresizing.net/images/logos/ImageIconPSD100.png";
            nvc["tags"] = "ImageResizer ImageResizing.Net Resize Resizer Resizing Crop Cropper Cropping automatic jCrop " +
            "asp:Image Photo Image Rotate Flip Drawing System.Drawing WIC WPF disk caching jpeg jpg gif png ASP.NET MVC IIS " +
            "transparency octree quanitization animated gifs dithering " +
            "Gaussian blur sharpen sharpening radius contrast saturation hue brightness histogram sepia grayscale invert color " +
            "pixel shader plugins noise removal exif rotation autorotate azure azurereader worker blob blobstore zip batch compress cache-control expires " +
            "amazon cloudfront s3 quality jpeg format drop shadow 404 handling url rewriting gradient freeimage " +
            "CatmullRom Lanczos3 bspline box bicubic bilinear CRW CR2 NEF RAF DNG MOS KDC DCR " +
            "404 redirect actionresult routing logging nlog psd remote url download webclient virtual path provider virtualpathprovider CAIR seam carving " +
            "content aware image resizing alpha channel grayscale y ry ntsc bt709 flat size limit sizelimiting getthumbnailimage bitmap SQL database query blob " +
            "watermark virtual folder text overlay image watermark automatic whitespace trimming product images thumbnails " + 
            "padding pad margin borders background color bgcolor InterpolationMode Fant wic IWICBitmap IWICBitmapSource";

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

            if (ask("Create or upload any NuGet packages?")) {

                say("For each nuget package, specify all operations to perform, then press enter. ");
                say("(c (create and overwrite), u (upload to nuget.org)");
                foreach (NPackageDescriptor desc in npackages) {

                    desc.VariableSubstitutions = GetNugetVariables();
                    desc.VariableSubstitutions["version"] = nugetVer;

                    desc.Version = nugetVer;

                    desc.OutputDirectory = Path.Combine(Path.Combine(f.ParentPath, "Releases", "nuget-packages"));
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
                bool success = BuildAll(true); //isMakingNugetPackage);

                //7 - Revert file to state at commit (remove 'full' version numbers and 'commit' value)
                v.Contents = fileContents;
                v.Save();

                if (!success) return; //If the build didn't go ok, pause and exit

                //8b - run cleanup routine
                RemoveUselessFiles();

                //Prepare searchersq
                PrepareForPackaging();

                //Allows use to temporarily edit all the sample project files
                using (RestorePoint rp = new RestorePoint(q.files(new Pattern("^/Samples/*/*.(cs|vb)proj$")))) {

                    //Replace all project references temporarily
                    foreach (string pf in q.files(new Pattern("^/Samples/[^/]+/*.(cs|vb)proj$"))) {
                        new ProjectFileEditor(pf).ReplaceAllProjectReferencesWithDllReferences("..\\..\\dlls\\release");
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
                    CannedAcl perm =  pd.Private ? CannedAcl.Private : CannedAcl.PublicRead;
                    say("Uploading " + Path.GetFileName(pd.Path) + " to " + bucketName + " with CannedAcl:" + perm.ToString());
                    bool retry = false;
                    do {
                        //Upload
                        try {
                            s3.AddObject(pd.Path, bucketName, Path.GetFileName(pd.Path), "application/zip", perm);
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
            extras.Run("/Clean Debug");
            extras.Run("/Clean Release");
            extras.Run("/Clean Trial");

        }

        public bool BuildAll(bool buildDebug) {
            int result = d.Run("/Build Release") + //Have to run Release first, since ImageResizerGUI includes the DLLs.
            d.Run("/Build Trial");
            if (buildDebug) result += d.Run("/Build Debug");

            int extrasResult =
            extras.Run("/Build Release") +
            extras.Run("/Build Trial");
            if (buildDebug) extrasResult += extras.Run("/Build Debug");

            if (result > 0 && !ask("There may have been build errors. Continue?")) return false;
            else if (extrasResult > 0 && !ask("There may have been build errors for Plugins With External Dependencies. Continue?")) return false;
            return true;
        }


        public void RemoveUselessFiles() {
            var f = new Futile(Console.Out);
            q = new FsQuery(this.f.ParentPath, new string[]{"/.git","^/Releases", "^/Tests/Builder"});


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
                "/.git","^/Releases","/Hidden/","^/Legacy","^/Tools/(Builder|BuildTools|docu)",
				"^/Samples/Images/(extra|private)/","/Thumbs.db$","/.DS_Store$",".suo$",".user$", "/._","/~$", 
                "^/Samples/MvcSample/App_Data/"

            };

        public void PrepareForPackaging() {
            if (q == null) q = new FsQuery(this.f.ParentPath, standardExclusions);
            //Don't copy XML or PDB files for the following libs:
            q.exclusions.Add(new Pattern("/(Newtonsoft.Json|DotNetZip|Aforge|LitS3|Ionic|NLog|MongoDB)*.(xml|pdb)$"));
            //Don't copy XML for these (but do keep pdb)
            q.exclusions.Add(new Pattern("/(AWSSDK|OpenCvSharp|FreeImageNet|Microsoft.)*.xml$"));
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
                p.Add(q.files("^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$"), "/");
                p.Add(q.files("^/readme.txt$"));
                p.Add(q.files("^/Core/license.txt$"), "");
                p.Add(q.files("^/Web.config$"));
            }
        }
        public void PackAllBinaries(PackageDescriptor desc) {
            using (var p = new Package(desc.Path, this.f.ParentPath)) {
                p.Add(q.files("^/dlls/release/*.(dll|pdb)$"), "/");
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
                p.Add(q.files("^/(core|samples|studiojs)/"));
                p.Add(q.files("^/[^/]+.txt$"));
                p.Add(q.files("^/Web.config$"));
            }
            q.exclusions = old;
        }







    }
}
