#r @"..\..\packages\FAKE\tools\FakeLib.dll"
#r @"..\..\packages\SharpZipLib\lib\20\ICSharpCode.SharpZipLib.dll"
#r @"..\..\packages\AWSSDK.2.2.4.0\lib\net45\AWSSDK.dll"

#load "FsQuery.fs"
#load "ZipHelper2.fs"
#load "Nuget.fs"

open Fake
open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open FsQuery
open FakeBuilder
open Amazon.S3.Transfer

let rootDir = Path.GetFullPath(__SOURCE_DIRECTORY__ + "/../..") + "\\"
let coreDir = rootDir + "Core/"
let mainSolution = coreDir + "ImageResizer.sln"
let assemblyInfoFile = coreDir + "SharedAssemblyInfo.cs"


// Settings

let fb_nuget_url = EnvironmentHelper.environVar "fb_nuget_url"
let fb_nuget_key = EnvironmentHelper.environVar "fb_nuget_key"

let fb_s3_bucket = EnvironmentHelper.environVar "fb_s3_bucket"
let fb_s3_id = EnvironmentHelper.environVar "fb_s3_id"
let fb_s3_key = EnvironmentHelper.environVar "fb_s3_key"



// AssemblyInfo

let mutable assemblyInfo = File.ReadAllText(assemblyInfoFile, UTF8Encoding.UTF8)

// replaces or appends assembly info
let setInfo info key value : string =
    let pattern = "(\\[assembly:\\s+" + key + "[^\]]*\\])"
    let replace = "[assembly: " + key + "(\"" + value + "\")]"
    let result = Regex.Replace(info, pattern, replace)
    // replace might have worked, but matches source
    if result.Contains(replace) then result
    else info + replace + "\n"

let getInfo info key : string =
    let pattern = "\\[assembly:\\s+" + key + "\\s*\\(\"([^\"]*)"
    let m = Regex.Match(info, pattern)
    if m.Success then m.Groups.[1].Value
    else ""

let mutable version = getInfo assemblyInfo "AssemblyVersion"
version <- version.Replace(".*", "")
if AppVeyor.AppVeyorEnvironment.BuildVersion <> null then
    version <- AppVeyor.AppVeyorEnvironment.BuildVersion



// Zip packing

let inventory = FsInventory(rootDir)
let excludes = toPatterns ["/.git";"^/Releases";"/Hidden/";"^/Legacy";"^/Tools/(Builder|BuildTools|docu)"; "^/submodules/docu";
                "^/Samples/Images/(extra|private)/";"/Thumbs.db$";"/.DS_Store$";".suo$";".cache$";".user$"; "/._";"/~$"; 
                "^/Samples/MvcSample/App_Data/"]

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
    ++ (rootDir + "Releases/*.zip")



// Targets

Target "Clean" (fun _ ->
    MSBuild "" "Clean" ["Configuration","Release"] [mainSolution] |> ignore
    MSBuild "" "Clean" ["Configuration","Debug"] [mainSolution] |> ignore
    MSBuild "" "Clean" ["Configuration","Trial"] [mainSolution] |> ignore
    CleanDirs [rootDir + "dlls/release"]
    CleanDirs [rootDir + "dlls/debug"]
    CleanDirs [rootDir + "dlls/trial"]
)

Target "Build" (fun _ ->
    MSBuild "" "Build" ["Configuration","Release"] [mainSolution] |> ignore
    MSBuild "" "Build" ["Configuration","Debug"] [mainSolution] |> ignore
    MSBuild "" "Build" ["Configuration","Trial"] [mainSolution] |> ignore
)

Target "PatchInfo" (fun _ ->
    let commit = Git.Information.getCurrentSHA1 ".."
    if commit <> null then
        assemblyInfo <- setInfo assemblyInfo "Commit" commit
    
    if version <> null then
        assemblyInfo <- setInfo assemblyInfo "AssemblyVersion" version
        assemblyInfo <- setInfo assemblyInfo "AssemblyFileVersion" version
        assemblyInfo <- setInfo assemblyInfo "AssemblyInformationalVersion" (version.Replace('.', '-'))
        assemblyInfo <- setInfo assemblyInfo "NugetVersion" version
    
    File.WriteAllText(assemblyInfoFile, assemblyInfo, UTF8Encoding.UTF8)
)

Target "PackNuget" (fun _ ->
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

Target "PackZips" (fun _ ->
    for file in deletableFiles do
        DeleteFile file
    
    let publish =
        if fb_s3_key = null then false
        elif fb_s3_key = "" then false
        else true
    
    let mutable query = FsQuery(inventory, excludes)
    
    query <- query.exclude("/(Newtonsoft.Json|DotNetZip|Aforge|LitS3|Ionic|NLog|MongoDB|Microsoft.|AWSSDK)*.(xml|pdb)$")
    query <- query.exclude("/(OpenCvSharp|FreeImageNet)*.xml$")
    query <- query.exclude("/(FreeImage|gsdll32|gsdll64).dll$")
    query <- query.exclude("/ImageResizerGUI.exe$")
    query <- query.exclude("_ReSharper")
    query <- query.exclude("^/Contrib/*/(bin|obj|imagecache|uploads|results)/*")
    query <- query.exclude("^/(Tests|Plugins|Samples)/*/(bin|obj|imagecache|uploads|hidden|results)/")
    query <- query.exclude("^/Core(.Mvc)?/obj/")
    query <- query.exclude("^/Tests/binaries")
    query <- query.exclude("^/Tests/LibDevCassini")
    query <- query.exclude("^/Tests/ComparisonBenchmark/Images")
    query <- query.exclude("^/Samples/SqlReaderSampleVarChar")
    query <- query.exclude(".config.transform$")
    query <- query.exclude("^/Plugins/Libs/FreeImage/Examples/")
    query <- query.exclude("^/Plugins/Libs/FreeImage/Wrapper/(Delphi|VB6|FreeImagePlus)")
    query <- query.exclude("^/Plugins/Libs/FreeImage/Wrapper/FreeImage.NET/cs/[^L]*/")
    
    let outDir =
        rootDir + "Releases/"
    
    let makename rtype =
        outDir + "Resizer" + (version.Replace('.', '-')) + "-" + rtype + "-" + (DateTime.UtcNow.ToString("MMM-d-yyyy")) + ".zip"
    
    // packmin
    let minname = sprintf "Resizer4-0-0-allbinaries-dec-2-2014"
    let mutable minfiles =
        List.map (fun x -> CustomFile(x, (Path.GetFileName(x)), false))
            (query.files(["^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$"; "^/Core/license.txt$"]))
    
    minfiles <-
        List.append minfiles
            (List.map (fun x -> CustomFile(x, snd((tupleRelative rootDir [x]).[0]), false))
            (query.files(["^/readme.txt$"; "^/Web.config$"])))
    
    CreateZip rootDir (makename "min") "" 7 false minfiles
    
    
    // packbin
    let mutable binfiles =
        List.map (fun x -> CustomFile(x, (Path.GetFileName(x)), false))
            (query.files("^/[^/]+.txt$"))
    
    binfiles <-
        List.append binfiles
            (List.map (fun x -> CustomFile(x, snd((tupleRelative (rootDir+"dlls\\release\\") [x]).[0]), false))
            (query.files("^/dlls/release/*.(dll|pdb)$")))
    
    CreateZip rootDir (makename "allbinaries") "" 7 false binfiles
    
    
    // packfull
    let mutable fullfiles =
        List.map (fun x -> CustomFile(x, snd((tupleRelative rootDir [x]).[0]), false))
            (query.files(["^/(core|contrib|core.mvc|plugins|samples|tests|studiojs)/"; "^/tools/COMInstaller";
                "^/dlls/(debug|release)"; "^/submodules/(lightresize|libwebp-net)"; "^/[^/]+.txt$"; "^/Web.config$"]))
    
    fullfiles <-
        List.append fullfiles
            (List.map (fun x -> CustomFile(x, (Path.GetFileName(x)), false))
            (query.files("^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$")))
    
    fullfiles <-
        List.append fullfiles
            (List.map (fun x -> CustomFile(x, ("StudioJS/" + snd((tupleRelative (rootDir+"submodules\\studiojs\\") [x]).[0])), false))
            (query.files("^/submodules/studiojs")))
    
    CreateZip rootDir (makename "full") "" 7 false fullfiles
    
    
    // packstandard
    query <- query.exclude("^/Core/[^/]+.sln")
    let mutable standard =
        List.map (fun x -> CustomFile(x, snd((tupleRelative rootDir [x]).[0]), false))
            (query.files(["^/dlls/(debug|release)/"; "^/(core|samples)/"; "^/[^/]+.txt$"; "^/Web.config$"]))
    
    standard <-
        List.append standard
            (List.map (fun x -> CustomFile(x, (Path.GetFileName(x)), false))
            (query.files("^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$")))
    
    standard <-
        List.append standard
            (List.map (fun x -> CustomFile(x, ("StudioJS/" + snd((tupleRelative (rootDir+"submodules\\studiojs\\") [x]).[0])), false))
            (query.files("^/submodules/studiojs")))
    
    CreateZip rootDir (makename "standard") "" 7 false standard
    
    ()
)

Target "PushNuget" (fun _ ->
    let symbolServ =
        if fb_nuget_url.Contains("myget.org") then "http://nuget.gw.SymbolSource.org/MyGet/"
        else ""
    
    for nuPkg in Directory.GetFiles(rootDir + "Releases/nuget-packages", "*.nupkg") do
        if not (nuPkg.Contains(".symbols.nupkg")) then
            Nuget.push nuPkg fb_nuget_url fb_nuget_key
        elif symbolServ <> "" then
            Nuget.push nuPkg symbolServ fb_nuget_key
)

Target "PushS3" (fun _ ->
    let s3config = new Amazon.S3.AmazonS3Config()
    s3config.Timeout <- System.Nullable (TimeSpan.FromHours(12.0))
    s3config.RegionEndpoint <- Amazon.RegionEndpoint.USEast1
    let s3client = new Amazon.S3.AmazonS3Client(fb_s3_id, fb_s3_key, s3config)
    let s3 = new TransferUtility(s3client)
    
    for zipPkg in Directory.GetFiles(rootDir + "Releases", "*.zip") do
        let request = new TransferUtilityUploadRequest()
        request.CannedACL <- Amazon.S3.S3CannedACL.PublicRead
        request.BucketName <- fb_s3_bucket
        request.ContentType <- "application/zip"
        request.Key <- Path.GetFileName(zipPkg)
        request.FilePath <- zipPkg
        
        printf "Uploading %s to S3/%s...\n" (Path.GetFileName(zipPkg)) fb_s3_bucket
        s3.Upload(request)
)

Target "Pack" (fun _ ->
    Run "PackNuget"
    Run "PackZips"
)

Target "Push" (fun _ ->
    if fb_nuget_key <> null && fb_nuget_key <> "" then
        Run "PushNuget"
    else
        printf "No nuget server information present, skipping push\n"
    
    if fb_s3_key <> null && fb_s3_key <> "" then
        Run "PushS3"
    else
        printf "No s3 server information present, skipping push\n"
)

"Clean"
==> "PatchInfo"
==> "Build"
==> "Pack"
==> "Push"

RunTargetOrDefault "Push"
