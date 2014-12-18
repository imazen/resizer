#r @"..\..\packages\FAKE.3.11.0\tools\FakeLib.dll"
#r @"..\..\packages\SharpZipLib.0.86.0\lib\20\ICSharpCode.SharpZipLib.dll"
#r @"..\..\packages\AWSSDK.2.2.4.0\lib\net45\AWSSDK.dll"

#load "FsQuery.fs"
#load "ZipHelper2.fs"
#load "Nuget.fs"
#load "AssemblyPatcher.fs"

open Fake
open Amazon.S3.Transfer
open ICSharpCode.SharpZipLib.Zip
open ICSharpCode.SharpZipLib.Core

open FsQuery
open FakeBuilder
open StringHelper
open XUnit2Helper

open System
open System.IO
open System.Text
open System.Text.RegularExpressions


// Settings

let envlist = ["fb_nuget_url"; "fb_nuget_key";
               "fb_s3_bucket"; "fb_s3_id"; "fb_s3_key"; "fb_pub_url";
               "fb_asmver"; "fb_filever"; "fb_infover"; "fb_nugetver";]

let settings = seq {for x in envlist -> x, (EnvironmentHelper.environVar x)} |> Map.ofSeq


let rootDir = Path.GetFullPath(__SOURCE_DIRECTORY__ + "/../..") + "\\"
let coreDir = rootDir + "Core/"
let mainSolution = rootDir + "AppVeyor.sln"
let fastScaleSln = rootDir + "Plugins/FastScaling/ImageResizer.Plugins.FastScaling.sln"
let assemblyInfoFile = coreDir + "SharedAssemblyInfo.cs"

let mutable version = AssemblyPatcher.getInfo assemblyInfoFile "AssemblyVersion"
version <- version.Replace(".*", "")
if AppVeyor.AppVeyorEnvironment.BuildVersion <> null then
    version <- AppVeyor.AppVeyorEnvironment.BuildVersion


// Default build settings

let setParams defaults =
        { defaults with
            Verbosity = Some(Quiet)
            Targets = ["Build"]
            Properties =
                [
                    "Optimize", "True"
                    "DebugSymbols", "True"
                    "Platform", "Any CPU"
                ]
        }

MSBuildDefaults <- setParams MSBuildDefaults


// Targets

Target "clean" (fun _ ->
    MSBuild "" "Clean" ["Configuration","Release"] [mainSolution] |> ignore
    MSBuild "" "Clean" ["Configuration","Debug"] [mainSolution] |> ignore
    MSBuild "" "Clean" ["Configuration","Trial"] [mainSolution] |> ignore
    
    CleanDirs [rootDir + "dlls/release"]
    CleanDirs [rootDir + "dlls/debug"]
    CleanDirs [rootDir + "dlls/trial"]
)

Target "build" (fun _ ->
    MSBuild "" "Build" ["Configuration","Release"] [mainSolution] |> ignore
    MSBuild "" "Build" ["Configuration","Debug"] [mainSolution] |> ignore
    MSBuild "" "Build" ["Configuration","Trial"] [mainSolution] |> ignore
    
    MSBuild "" "Build" ["Configuration","Release"; "Platform","x86"] [fastScaleSln] |> ignore
    MSBuild "" "Build" ["Configuration","Debug"; "Platform","x86"] [fastScaleSln] |> ignore
    MSBuild "" "Build" ["Configuration","Release"; "Platform","x64"] [fastScaleSln] |> ignore
    MSBuild "" "Build" ["Configuration","Debug"; "Platform","x64"] [fastScaleSln] |> ignore
)

Target "patch_commit" (fun _ ->
    let commit = Git.Information.getCurrentSHA1 ".."
    if commit <> null then
        AssemblyPatcher.setInfo assemblyInfoFile ["Commit", commit]
)

Target "patch_ver" (fun _ ->
    if version <> null then
        AssemblyPatcher.setInfo assemblyInfoFile ["AssemblyVersion", version;
            "AssemblyFileVersion", version;
            "AssemblyInformationalVersion",(version.Replace('.', '-'));
            "NugetVersion", version]
)

Target "patch_info" (fun _ ->
    Run "patch_commit"
    Run "patch_ver"
)

Target "test" (fun _ ->
    let xunit = Seq.nth 0 (!! (rootDir + "Packages/xunit.runners*/tools/xunit.console.exe"))
    let xunit32 = replace "xunit.console.exe" "xunit.console.x86.exe" xunit
      
    !! (rootDir + "Tests/binaries/release/*Tests.dll")
        //++ (rootDir + "Tests/binaries/release/x64/*Tests.dll")
        ++ (rootDir + "Tests/binaries/release/x64/*Tests.Cpp.dll")
        -- (rootDir + "**/ImageResizer.Plugins.LicenseVerifier.Tests.dll")
        -- (rootDir + "**/ImageResizer.CoreFSharp.Tests.dll")
            |> xUnit (fun p -> {p with ToolPath = xunit})
    
    !! (rootDir + "Tests/binaries/release/*Tests.dll")
        //++ (rootDir + "Tests/binaries/release/x86/*Tests.dll")
        ++ (rootDir + "Tests/binaries/release/x86/*Tests.Cpp.dll")
        -- (rootDir + "**/ImageResizer.Plugins.LicenseVerifier.Tests.dll")
        -- (rootDir + "**/ImageResizer.CoreFSharp.Tests.dll")
        -- (rootDir + "**/ImageResizer.AllPlugins.Tests.dll")
        -- (rootDir + "**/ImageResizer.CopyMetadata.Tests.dll")
        -- (rootDir + "**/ImageResizer.Plugins.TinyCache.Tests.dll")
            |> xUnit (fun p -> {p with ToolPath = xunit32})
)

Target "pack_nuget" (fun _ ->
    CleanDirs [rootDir + "tmp"]
    CleanDirs [rootDir + "Releases/nuget-packages"]
    
    let nvc = ["author", "Nathanael Jones, Imazen";
           "owners", "nathanaeljones, imazen";
           "pluginsdlldir", (rootDir+"dlls/trial");
           "coredlldir", (rootDir+"dlls/release");
           "iconurl", "http://imageresizing.net/images/logos/ImageIconPSD100.png";
           "plugins", "## 30+ plugins available\n\n" + 
               "Search 'ImageResizer' on nuget.org, or visit imageresizing.net to see 40+ plugins, including WPF, WIC, FreeImage, OpenCV, AForge &amp; Ghostscript (PDF) integrations. " + 
               "You'll also find  plugins for disk caching, memory caching, Microsoft SQL blob support, Amazon CloudFront, S3, Azure Blob Storage, MongoDB GridFS, automatic whitespace trimming, " +
               "automatic white balance, octree 8-bit gif/png quantization and transparency dithering, animated gif resizing, watermark &amp; text overlay support, content aware image resizing /" + 
               " seam carving (based on CAIR), grayscale, sepia, histogram, alpha, contrast, saturation, brightness, hue, Guassian blur, noise removal, and smart sharpen filters, psd editing &amp; " +
               "rendering, raw (CR2, NEF, DNG, etc.) file exposure, .webp (weppy) support, image batch processing &amp; compression into .zip archives, red eye auto-correction,  face detection, and " + 
               "secure (signed!) remote HTTP image processing. Most datastore plugins support the Virtual Path Provider system, and can be used for non-image files as well.\n\n" ]
    
    
    // replace nuget vars
    for nuSpec in Directory.GetFiles(rootDir + "nuget", "*.nuspec") do
        Nuget.fillVariables nuSpec (rootDir+"tmp/"+Path.GetFileName(nuSpec)) nvc
    
    // process symbol packages first (as they need to be renamed)
    for nuSpec in Directory.GetFiles(rootDir + "tmp", "*.symbols.nuspec") do
        Nuget.pack nuSpec version (rootDir + "Releases/nuget-packages")
        let baseName = rootDir + "Releases/nuget-packages/" + Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(nuSpec)) + "." + version
        File.Move(baseName + ".nupkg", baseName + ".symbols.nupkg")
    
    // process regular packages
    for nuSpec in Directory.GetFiles(rootDir + "tmp", "*.nuspec") do
        if not (nuSpec.Contains(".symbols.nuspec")) then
            Nuget.pack nuSpec version (rootDir + "Releases/nuget-packages")
    
    // remove any mess
    DeleteDir (rootDir + "tmp")
)

Target "pack_zips" (fun _ ->
    for file in !! (rootDir + "Releases/*.zip") do
        DeleteFile file
    
    let inventory = FsInventory(rootDir)
    let mutable query = FsQuery(inventory, (toPatterns ["/.git";"^/Releases";"/Hidden/";"^/Legacy";"^/Tools/(Builder|BuildTools|docu)";
        "^/submodules/docu"; "^/Samples/Images/(extra|private)/";"/Thumbs.db$";"/.DS_Store$";".suo$";".cache$";".user$"; "/._";"/~$"; 
        "^/Samples/MvcSample/App_Data/"]))
    
    query <- query.exclude(["/(Newtonsoft.Json|DotNetZip|Aforge|LitS3|Ionic|NLog|MongoDB|Microsoft.|AWSSDK)*.(xml|pdb)$";
        "/(OpenCvSharp|FreeImageNet)*.xml$"; "/(FreeImage|gsdll32|gsdll64).dll$";
        "/ImageResizerGUI.exe$";
        "_ReSharper";
        "^/Contrib/*/(bin|obj|imagecache|uploads|results)/*";
        "^/(Tests|Plugins|Samples)/*/(bin|obj|imagecache|uploads|hidden|results)/";
        "^/Core(.Mvc)?/obj/";
        "^/Tests/binaries";
        "^/Tests/LibDevCassini";
        "^/Tests/ComparisonBenchmark/Images";
        "^/Samples/SqlReaderSampleVarChar";
        ".config.transform$";
        "^/Plugins/Libs/FreeImage/Examples/";
        "^/Plugins/Libs/FreeImage/Wrapper/(Delphi|VB6|FreeImagePlus)";
        "^/Plugins/Libs/FreeImage/Wrapper/FreeImage.NET/cs/[^L]*/"])
    
    let outDir = rootDir + "Releases/"
    let makeName rtype = outDir + "Resizer" + (version.Replace('.', '-')) + "-" + rtype + "-" + (DateTime.UtcNow.ToString("MMM-d-yyyy")) + ".zip"
    
    let toZipEntries (q : FsQuery) (patterns : string list) (baseDir : string) (targetDir : string) (inRoot : bool) =
        let files = q.files(patterns)
        
        let tmpInv = FsInventory(rootDir)
        let transFiles = FsQuery(tmpInv, []).files("^/Samples/*/*.(cs|vb)proj$")
        
        let tFiles = List.filter (fun x -> (List.exists ((=) x) transFiles)) files
        let nFiles = List.filter (fun x -> not (List.exists ((=) x) transFiles)) files
        
        let find = @"<ProjectReference.*?<Name>(.*?)</Name>.*?</ProjectReference>"
        let replace = "<Reference Include=\"$1\"><HintPath>..\\..\\dlls\\release\$1.dll</HintPath></Reference>"
        
        if not inRoot then
            List.append
                (List.map (fun x -> CustomFile(x, (targetDir + snd((tupleRelative baseDir [x]).[0])), true)) nFiles)
                (List.map (fun x -> CustomFileTransform(x, (targetDir + snd((tupleRelative baseDir [x]).[0])), true, find, replace)) tFiles)
        else
            List.append
                (List.map (fun x -> CustomFile(x, (Path.GetFileName(x)), true)) nFiles)
                (List.map (fun x -> CustomFileTransform(x, (targetDir + snd((tupleRelative baseDir [x]).[0])), true, find, replace)) tFiles)
    
    
    // packmin
    let minfiles = toZipEntries query ["^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$";
        "^/Core/license.txt$"; "^/readme.txt$"; "^/Web.config$"] "" "" true
    CreateZip rootDir (makeName "min") "" 5 false minfiles
    
    
    // packbin
    let mutable binfiles = toZipEntries query ["^/[^/]+.txt$"] "" "" true
    binfiles <- List.append binfiles (toZipEntries query ["^/dlls/release/*.(dll|pdb)$"] (rootDir+"dlls\\release\\") "" false)
    CreateZip rootDir (makeName "allbinaries") "" 5 false binfiles
    
    
    // packfull
    let mutable fullfiles = toZipEntries query ["^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$"] "" "" true
    fullfiles <- List.append fullfiles (toZipEntries query
        ["^/(core|contrib|core.mvc|plugins|samples|tests|studiojs)/"; "^/tools/COMInstaller"; "^/dlls/(debug|release)";
        "^/submodules/(lightresize|libwebp-net)"; "^/[^/]+.txt$"; "^/Web.config$"] rootDir "" false)
    fullfiles <- List.append fullfiles (toZipEntries query ["^/submodules/studiojs"] (rootDir+"submodules\\studiojs") "StudioJS" false)
    CreateZip rootDir (makeName "full") "" 5 false fullfiles
    
    
    // packstandard
    query <- query.exclude("^/Core/[^/]+.sln")
    let mutable standard = toZipEntries query ["^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$"] "" "" true
    standard <- List.append standard (toZipEntries query ["^/dlls/(debug|release)/"; "^/(core|samples)/";
        "^/[^/]+.txt$"; "^/Web.config$"] rootDir "" false)
    standard <- List.append standard (toZipEntries query ["^/submodules/studiojs"] (rootDir+"submodules\\studiojs") "StudioJS" false)
    CreateZip rootDir (makeName "standard") "" 5 false standard
    
    ()
)

Target "push_nuget" (fun _ ->
    let symbolServ =
        if settings.["fb_nuget_url"].Contains("myget.org") then "http://nuget.gw.SymbolSource.org/MyGet/"
        else ""
    
    for nuPkg in Directory.GetFiles(rootDir + "Releases/nuget-packages", "*.nupkg") do
        if not (nuPkg.Contains(".symbols.nupkg")) then
            Nuget.push nuPkg settings.["fb_nuget_url"] settings.["fb_nuget_key"]
        elif symbolServ <> "" then
            Nuget.push nuPkg symbolServ settings.["fb_nuget_key"]
)

Target "push_zips" (fun _ ->
    let s3config = new Amazon.S3.AmazonS3Config()
    s3config.Timeout <- System.Nullable (TimeSpan.FromHours(12.0))
    s3config.RegionEndpoint <- Amazon.RegionEndpoint.USEast1
    let s3client = new Amazon.S3.AmazonS3Client(settings.["fb_s3_id"], settings.["fb_s3_key"], s3config)
    let s3 = new TransferUtility(s3client)
    
    for zipPkg in Directory.GetFiles(rootDir + "Releases", "*.zip") do
        let mutable tries = 3
        let request = new TransferUtilityUploadRequest()
        request.CannedACL <- Amazon.S3.S3CannedACL.PublicRead
        request.BucketName <- settings.["fb_s3_bucket"]
        request.ContentType <- "application/zip"
        request.Key <- Path.GetFileName(zipPkg)
        request.FilePath <- zipPkg
        
        while tries > 0 do
            try
                printf "Uploading %s to S3/%s...\n" (Path.GetFileName(zipPkg)) settings.["fb_s3_bucket"]
                s3.Upload(request)
                tries <- 0
            with exn ->
                tries <- tries-1
                if tries=0 then
                    raise exn
)

Target "pack" (fun _ ->
    Run "pack_nuget"
    Run "pack_zips"
)

Target "push" (fun _ ->
    if settings.["fb_nuget_key"] <> null && settings.["fb_nuget_key"] <> "" then
        Run "push_nuget"
    else
        printf "No nuget server information present, skipping push\n"
    
    if settings.["fb_s3_key"] <> null && settings.["fb_s3_key"] <> "" then
        Run "push_zips"
    else
        printf "No s3 server information present, skipping push\n"
)

Target "unmess" (fun _ ->
    let deletableFiles =
        !! (rootDir + "Tests/binaries/**")
        ++ (rootDir + "Core/obj/**")
        ++ (rootDir + "Plugins/**/obj/**")
        ++ (rootDir + "Samples/MvcSample/App_Data/**")
        ++ (rootDir + "dlls/**/Aforge*.pdb")
        ++ (rootDir + "dlls/**/Aforge*.xml")
        ++ (rootDir + "dlls/**/LitS3*.pdb")
        ++ (rootDir + "dlls/**/LitS3*.xml")
        ++ (rootDir + "dlls/**/Ionic*.pdb")
        ++ (rootDir + "dlls/**/Ionic*.xml")
        ++ (rootDir + "**/Thumbs.db")
        ++ (rootDir + "**/.DS_Store")
    
    for file in deletableFiles do
        DeleteFile file
)

Target "print_stats" (fun _ ->
    for zipPkg in Directory.GetFiles(rootDir + "Releases", "*.zip") do
        printf "\nLarge files in %s:\n" (Path.GetFileName(zipPkg))
        let zip = new ZipInputStream(File.OpenRead(zipPkg))
        let mutable entry = zip.GetNextEntry()
        while entry <> null do
            if (int entry.CompressedSize) > 300 * 1024 then
                printf "%dk %s\n" (entry.CompressedSize/1024L) entry.Name
            entry <- zip.GetNextEntry()
    
    
    if settings.["fb_pub_url"] <> null then
        printf "\nDownload urls:\n"
        for zipPkg in Directory.GetFiles(rootDir + "Releases", "*.zip") do
            printf "%s/%s\n" settings.["fb_pub_url"] (Path.GetFileName(zipPkg))
)

Target "custom" (fun _ ->
    let targets = getBuildParamOrDefault "targets" ""
    let tlist = List.map (fun x -> (split ' ' x).[0]) (split ';' targets)
    
    if tlist.Length > 0 then
        for i=0 to tlist.Length-2 do
            tlist.[i] ==> tlist.[i+1]
        Run tlist.[tlist.Length-1]
)

Target "do_all" (fun _ ->
    "clean"
    ==> "patch_info"
    ==> "build"
    ==> "test"
    ==> "unmess"
    ==> "pack"
    ==> "push"
    ==> "print_stats"
    
    Run "print_stats"
)

RunTargetOrDefault "do_all"
