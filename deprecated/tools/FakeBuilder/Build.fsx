// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
#r @"packages/FAKE/tools/FakeLib.dll"
#r @"packages/SharpZipLib/lib/20/ICSharpCode.SharpZipLib.dll"
// #r @"packages/AWSSDK.S3/lib/net45/AWSSDK.dll"
#r @"packages/AWSSDK.Core/lib/net45/AWSSDK.Core.dll"
#r @"packages/AWSSDK.S3/lib/net45/AWSSDK.S3.dll"

#load "AssemblyPatcher.fs"
#load "FsQuery.fs"
#load "Nuget.fs"
#load "SemVerHelper2.fs"
#load "ZipHelper2.fs"


open Fake
open Amazon.S3.Transfer
open ICSharpCode.SharpZipLib.Zip
open ICSharpCode.SharpZipLib.Core

open EnvironmentHelper
open FileUtils
open FsQuery
open FakeBuilder
open Git.CommandHelper
open ProcessHelper
open SemVerHelper
open SemVerHelper2
open StringHelper
open XUnit2Helper

open System
open System.IO
open System.Text
open System.Text.RegularExpressions

printfn "Build.fsx starting.."
// Settings

let variableList = ["fb_nuget_url"; "fb_nuget_key";
                    "fb_s3_bucket"; "fb_s3_id"; "fb_s3_key"; "fb_pub_url";
                    "fb_nuget_rel_url"; "fb_nuget_rel_key";
                    "fb_s3_rel_bucket"; "fb_s3_rel_id"; "fb_s3_rel_key"; "license_keys";
                    "fb_imageserver_repo"; "fb_imageserver_branch"; "fb_imageserver_rel_branch"; "fb_imageserver_path";]

let mutable settings = seq {for x in variableList -> x, (environVar x)} |> Map.ofSeq


printfn "Values present for configuration keys %A\n"   (variableList |>  Seq.filter(fun x -> x |> environVar |> isNullOrEmpty |> not))

printfn "Missing or emtpy value for keys %A\n" (variableList |>  Seq.filter(fun x -> x |> environVar |> isNullOrEmpty))



let fixSlashes (s) = 
  Regex("[\\\\/]").Replace(s, System.IO.Path.DirectorySeparatorChar.ToString())

let rootDir = fixSlashes(Path.GetFullPath(__SOURCE_DIRECTORY__ + "/../..") + "\\")
let coreDir = fixSlashes(rootDir + "Core/")
let mainSolution = rootDir + "AppVeyor.sln"
let fastScaleSln = rootDir + fixSlashes("Plugins/FastScaling/ImageResizer.Plugins.FastScaling.sln")
let assemblyInfoFile = coreDir + "SharedAssemblyInfo.cs"
let assemblyInfoCppFile = rootDir + fixSlashes("Plugins/FastScaling/AssemblyInfo.cpp")

let isAutoBuild =
    if isNotNullOrEmpty (environVar "APPVEYOR") then true
    else false


// Versioning

let mutable version = parse (AssemblyPatcher.getInfo assemblyInfoFile "AssemblyInformationalVersion")
let buildNo =
    if isNotNullOrEmpty (environVar "APPVEYOR_BUILD_NUMBER") then environVar "APPVEYOR_BUILD_NUMBER"
    else (DateTime.UtcNow.TimeOfDay.Milliseconds % int Int16.MaxValue).ToString()
let prerel =
    if version.PreRelease <> None then version.PreRelease.Value.Origin
    else "prerelease"
version <-
    { version with
        Build = buildNo
        PreRelease = PreRelease.TryParse prerel
    }

let mutable isRelease = false
let mutable releaseVersionString = ""

type System.String with
    member s1.icompare(s2: string) =
      System.String.Equals(s1, s2, System.StringComparison.CurrentCultureIgnoreCase);;

let trimV (s) = 
    Regex("^v").Replace(s, "")
    
let git_tag = 
  if isAutoBuild && "True".icompare(environVar "APPVEYOR_REPO_TAG") then 
    Some(environVar "APPVEYOR_REPO_TAG_NAME")
  else 
    let ok,msg,errors = runGitCommand "" "describe --tags --exact-match --abbrev=0"
    if ok && msg.Count > 0 then
      Some(msg.[0])
    else
      None

match git_tag with
| Some(s) when isValidSemVer(trimV s) -> 
  isRelease <- true
  releaseVersionString <- trimV s
  version <- parse(trimV(s))
  printf "Using git tag %s as the release version\n" s
| Some(s) -> printf "Warning: git tag '%s' is not a valid semver; not processing as a release\n" s
| None -> printf "No git tag found on this commit.\n"

 
let nugetVer = { version with Build = "" }



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
                    "NoWarn", "1591,1573,0618"
                ]
        }

MSBuildDefaults <- setParams MSBuildDefaults


// Targets

Target "clean" (fun _ ->
    MSBuild "" "Clean" ["Configuration","Release"] [mainSolution] |> ignore
    MSBuild "" "Clean" ["Configuration","Debug"] [mainSolution] |> ignore
    
    MSBuild "" "Clean" ["Configuration","Release"; "Platform","Win32"] [fastScaleSln] |> ignore
    MSBuild "" "Clean" ["Configuration","Debug"; "Platform","Win32"] [fastScaleSln] |> ignore
    MSBuild "" "Clean" ["Configuration","Release"; "Platform","x64"] [fastScaleSln] |> ignore
    MSBuild "" "Clean" ["Configuration","Debug"; "Platform","x64"] [fastScaleSln] |> ignore
    
    CleanDirs [rootDir + fixSlashes("dlls/release")]
    CleanDirs [rootDir + fixSlashes("dlls/debug")]
)

Target "build" (fun _ ->

    MSBuild "" "Build" ["Configuration","Release"] [mainSolution] |> ignore
    MSBuild "" "Build" ["Configuration","Release"; "Platform","Win32"] [fastScaleSln] |> ignore
    MSBuild "" "Build" ["Configuration","Release"; "Platform","x64"] [fastScaleSln] |> ignore
    )

Target "patch_commit" (fun _ ->
    let commit = Git.Information.getCurrentSHA1 ".."
    if commit <> null then
        AssemblyPatcher.setInfo assemblyInfoFile ["Commit", commit]
        AssemblyPatcher.setInfo assemblyInfoCppFile ["CommitAttribute", commit]
)

Target "patch_date" (fun _ ->
    let date = DateTimeOffset.UtcNow.ToString("o")
    AssemblyPatcher.setInfo assemblyInfoFile ["BuildDate", date]
    AssemblyPatcher.setInfo assemblyInfoCppFile ["BuildDateAttribute", date]
)

Target "patch_ver" (fun _ ->
    let asmVer =
        { version with
            Minor = 0
            Patch = 0
            Build = ""
            PreRelease = PreRelease.TryParse ""
        }
    
    let fileVer =
        { version with
            Build = ""
            PreRelease = PreRelease.TryParse ""
        }
    
    AssemblyPatcher.setInfo assemblyInfoFile [
        "AssemblyVersion", asmVer.ToString()+".0";
        "AssemblyFileVersion", fileVer.ToString()+"."+buildNo;
        "AssemblyInformationalVersion", version.ToString()]

    AssemblyPatcher.setInfo assemblyInfoCppFile [
        "AssemblyVersionAttribute", asmVer.ToString()+".0";
        "AssemblyFileVersionAttribute", fileVer.ToString()+"."+buildNo;
        "AssemblyInformationalVersionAttribute", version.ToString()]
)

Target "patch_info" (fun _ ->
    Run "patch_commit"
    Run "patch_ver"
    Run "patch_date"
)

Target "test" (fun _ ->
    let xunit = Seq.nth 0 (!! (rootDir + "Packages/xunit.runner.*/tools/xunit.console.exe"))
    let xunit32 = replace "xunit.console.exe" "xunit.console.x86.exe" xunit
      
    !! (rootDir + "Tests/binaries/release/*Tests.dll")
        //++ (rootDir + "Tests/binaries/release/x64/*Tests.dll")
        ++ (rootDir + "Tests/binaries/release/x64/ImageResizer.Plugins.FastScaling.Tests.dll")
        -- (rootDir + "**/ImageResizer.CoreFSharp.Tests.dll")
            |> xUnit2 (fun p -> {p with ToolPath = xunit; ExcludeTraits = Some("requiresmongo","true")})
            
    !! (rootDir + "Tests/binaries/release/*Tests.dll")
        //++ (rootDir + "Tests/binaries/release/x86/*Tests.dll")
        ++ (rootDir + "Tests/binaries/release/x86/ImageResizer.Plugins.FastScaling.Tests.dll")
        -- (rootDir + "**/ImageResizer.CoreFSharp.Tests.dll")
        -- (rootDir + "**/ImageResizer.AllPlugins.Tests.dll")
        -- (rootDir + "**/ImageResizer.CopyMetadata.Tests.dll")
        -- (rootDir + "**/ImageResizer.Plugins.TinyCache.Tests.dll")
            |> xUnit2 (fun p -> {p with ToolPath = xunit32; ExcludeTraits = Some("requiresmongo","true")})
)

Target "pack_nuget" (fun _ ->
    CleanDirs [rootDir + "tmp"]
    CleanDirs [rootDir + "Releases/nuget-packages"]
    
    let ver =
        if isRelease then nugetVer.ToString()
        else nugetVer.ToString() + sprintf "%04d" (int32 buildNo)
    
    let nvc = ["author", "Nathanael Jones, Imazen";
           "owners", "nathanaeljones, imazen";
           "pluginsdlldir", (rootDir+"dlls/release");
           "coredlldir", (rootDir+"dlls/release");
           "iconurl", "http://imageresizing.net/images/logos/ImageIconPSD100.png";
           "plugins", "## 30+ plugins available\n\n" + 
               "Search 'ImageResizer' on nuget.org, or visit imageresizing.net to see 40+ plugins. Some offer 4-30x performance improvements; some render PDFs and PSDs; others detect faces and trim whitespace.\n" + 
               "You'll find  plugins for disk caching, memory caching, Microsoft SQL blob support, Amazon CloudFront, S3, Azure Blob Storage, MongoDB GridFS, automatic whitespace trimming, " +
               "automatic white balance, octree 8-bit gif/png quantization and transparency dithering, animated gif resizing, watermark &amp; text overlay support, content aware image resizing /" + 
               " seam carving (based on CAIR), grayscale, sepia, histogram, alpha, contrast, saturation, brightness, hue, Guassian blur, noise removal, and smart sharpen filters, psd editing &amp; " +
               "rendering, raw (CR2, NEF, DNG, etc.) file exposure, .webp (weppy) support, image batch processing &amp; compression into .zip archives, red eye auto-correction,  face detection, and " + 
               "secure (signed!) remote HTTP image processing. Most datastore plugins support the Virtual Path Provider system, and can be used for non-image files as well.\n\n" ]
    
    
    // replace nuget vars
    for nuSpec in Directory.GetFiles(rootDir + "nuget", "*.nuspec") do
        Nuget.fillVariables nuSpec (rootDir+"tmp/"+Path.GetFileName(nuSpec)) nvc
    
    // process regular packages
    for nuSpec in Directory.GetFiles(rootDir + "tmp", "*.nuspec") do
        Nuget.pack nuSpec ver (rootDir + "Releases/nuget-packages")
    
    // remove any mess
    DeleteDir (rootDir + "tmp")
)

Target "pack_zips" (fun _ ->
    for file in !! (rootDir + "Releases/*.zip") do
        DeleteFile file
    
    let inventory = FsInventory(rootDir)

    let mutable query = FsQuery(inventory, (toPatterns ["/.git";"^/Releases";"/Hidden/";"^/Legacy";"^/Tools/(Builder|BuildTools|docu)";
        "^/submodules/docu"; "^/Samples/Images/(extra|private)/";"/Thumbs.db$"; "/._";"/~$";".FileAbsoluteList.txt$";"/(pthreadVSE2.dll|cair.exe|Nuget.exe)$";
        "^/Samples/MvcSample/App_Data/";"/ipch/";"_ReSharper";"_(i|p).c$";
        ".(DS_Store|pch|opensdf|sdf|sbr|unsuccessfulbuild|coverage|tlog|metagen|lastbuildstate|res|log|ilk|Cache|cache|bak|tlh|tlb|suo|ncb|vspscc|vssscc|aps|user|obj|coverage|user|userprefs)$"]))
    
    query <- query.exclude(["/(Newtonsoft.Json|DotNetZip|Aforge|LitS3|Ionic|NLog|MongoDB|Microsoft.|AWSSDK)*.(xml|pdb)$";
        "/(OpenCvSharp|FreeImageNet)*.xml$"; "/(FreeImage|gsdll32|gsdll64|pdfium).dll$";
        "/ImageResizerGUI.exe$";
        "_ReSharper";
        "^/Contrib/*/(bin|obj|imagecache|uploads|results)/*";
        "^/(Tests|Plugins|Samples)/*/(bin|obj|imagecache|uploads|hidden|results)/";
        "^/Core(.Mvc)?/obj/";
        "^/Tests/binaries";
        "^/Plugins/FastScaling/*.(lib|dll|xml|pdb|exe).?*";
        "^/Tests/LibDevCassini";
        "^/Tests/ComparisonBenchmark/Images";
        "^/Samples/SqlReaderSampleVarChar";
        ".config.transform$";
        "^/Plugins/Libs/FreeImage/Examples/";
        "^/Plugins/Libs/FreeImage/Wrapper/(Delphi|VB6|FreeImagePlus)";
        "^/Plugins/Libs/FreeImage/Wrapper/FreeImage.NET/cs/[^L]*/"])
    
    let outDir = rootDir + "Releases/"
    let extra_buildNo =
        if isRelease then ""
        else "-" + buildNo
    let makeName rtype = outDir + "Resizer" + version.Major.ToString() + "-" + version.Minor.ToString() + "-" + version.Patch.ToString() + extra_buildNo + "-" + rtype + "-" + (DateTime.UtcNow.ToString("MMM-d-yyyy")) + ".zip"
    
    let toZipEntries (q : FsQuery) (patterns : string list) (baseDir : string) (targetDir : string) (inRoot : bool) =
        let files = q.files(patterns)
        
        let tmpInv = FsInventory(rootDir)
        let transFiles = FsQuery(tmpInv, []).files("^/Samples/*/*.(cs|vb)proj$")
        
        let transFileList = List.filter (fun x -> (List.exists ((=) x) transFiles)) files
        let otherFileList = List.filter (fun x -> not (List.exists ((=) x) transFiles)) files
        
        let find = @"<ProjectReference.*?<Name>(.*?)</Name>.*?</ProjectReference>"
        let replace = "<Reference Include=\"$1\"><HintPath>..\\..\\dlls\\release\$1.dll</HintPath></Reference>"
        
        if not inRoot then
            List.append
                (List.map (fun x -> CustomFile(x, (targetDir + snd((tupleRelative baseDir [x]).[0])), true)) otherFileList)
                (List.map (fun x -> CustomFileTransform(x, (targetDir + snd((tupleRelative baseDir [x]).[0])), true, find, replace)) transFileList)
        else
            List.append
                (List.map (fun x -> CustomFile(x, (Path.GetFileName(x)), true)) otherFileList)
                (List.map (fun x -> CustomFileTransform(x, (targetDir + snd((tupleRelative baseDir [x]).[0])), true, find, replace)) transFileList)
    
    
    // packmin
    //let minfiles = toZipEntries query ["^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$";
    //    "^/Core/license.txt$"; "^/readme.txt$"; "^/Web.config$"] "" "" true
    //CreateZip rootDir (makeName "min") "" 5 false minfiles
    
    
    // packbin
    //let mutable binfiles = toZipEntries query ["^/[^/]+.txt$"] "" "" true
    //binfiles <- List.append binfiles (toZipEntries query ["^/dlls/release/*.(dll|pdb)$"] (rootDir+"dlls\\release\\") "" false)
    //CreateZip rootDir (makeName "allbinaries") "" 5 false binfiles
    
    
    // packfull
    let mutable fullfiles = toZipEntries query ["^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$"] "" "" true
    fullfiles <- List.append fullfiles (toZipEntries query
        ["^/(core|contrib|core.mvc|plugins|samples|tests|studiojs)/"; "^/tools/COMInstaller"; "^/dlls/(debug|release)";
        "^/submodules/(lightresize|libwebp-net)"; "^/[^/]+.txt$"; "^/Web.config$"] rootDir "" false)
    fullfiles <- List.append fullfiles (toZipEntries query ["^/submodules/studiojs"] (rootDir+"submodules\\studiojs") "StudioJS" false)
    CreateZip rootDir (makeName "full") "" 5 false fullfiles
    
    
    // packstandard
    // query <- query.exclude("^/Core/[^/]+.sln")
    //let mutable standard = toZipEntries query ["^/dlls/release/ImageResizer.(Mvc.)?(dll|pdb|xml)$"] "" "" true
    // standard <- List.append standard (toZipEntries query ["^/dlls/(debug|release)/"; "^/(core|samples)/";
    //"^/[^/]+.txt$"; "^/Web.config$"] rootDir "" false)
    // standard <- List.append standard (toZipEntries query ["^/submodules/studiojs"] (rootDir+"submodules\\studiojs") "StudioJS" false)
    //CreateZip rootDir (makeName "standard") "" 5 false standard

    ()
)

Target "push_nuget" (fun _ ->
    let (nuget_url, nuget_key) =
        if isRelease then (settings.["fb_nuget_rel_url"], settings.["fb_nuget_rel_key"])
        else (settings.["fb_nuget_url"], settings.["fb_nuget_key"])
    
    if isNullOrEmpty nuget_key then
        printf "No nuget information present, skipping push\n"
    else
        Paket.Push (fun p -> 
            { p with 
                ToolPath = "../.paket/paket.exe" 
                PublishUrl = nuget_url 
                WorkingDir = rootDir + "Releases/nuget-packages" 
                ApiKey = nuget_key}) 
)

Target "push_zips" (fun _ ->
    let (s3_id, s3_key, s3_bucket) =
        if isRelease then (settings.["fb_s3_rel_id"], settings.["fb_s3_rel_key"], settings.["fb_s3_rel_bucket"])
        else (settings.["fb_s3_id"], settings.["fb_s3_key"], settings.["fb_s3_bucket"])
    
    if isNullOrEmpty s3_key then
        printf "No s3 server information present, skipping push\n"
    else
        
        let s3config = new Amazon.S3.AmazonS3Config()
        s3config.Timeout <- System.Nullable (TimeSpan.FromHours(12.0))
        s3config.RegionEndpoint <- Amazon.RegionEndpoint.USEast1
        let s3client = new Amazon.S3.AmazonS3Client(s3_id, s3_key, s3config)
        let s3 = new TransferUtility(s3client)
        
        for zipPkg in Directory.GetFiles(rootDir + "Releases", "*.zip") do
            let mutable tries = 3
            let drm_free = (settings.["license_keys"] = "false")
            let request = new TransferUtilityUploadRequest()
            request.CannedACL <- if drm_free then  Amazon.S3.S3CannedACL.Private else Amazon.S3.S3CannedACL.PublicRead
            request.BucketName <- s3_bucket
            request.ContentType <- "application/zip"
            request.Key <- Path.GetFileName(zipPkg).Replace(".zip", if drm_free then "-drm-free.zip" else ".zip")
            request.FilePath <- zipPkg
            
            while tries > 0 do
                try
                    printf "Uploading %s to S3/%s...\n" (request.Key) s3_bucket
                    s3.Upload(request)
                    tries <- 0
                with exn ->
                    tries <- tries-1
                    if tries=0 then
                        reraise()
)

Target "update_imageserv" (fun _ ->
    let img_repo = settings.["fb_imageserver_repo"]
    let img_path = settings.["fb_imageserver_path"]
    let img_branch =
        if isRelease then settings.["fb_imageserver_rel_branch"]
        else settings.["fb_imageserver_branch"]
    
    let ver =
        if isRelease then nugetVer.ToString()
        else nugetVer.ToString() + sprintf "%04d" (int32 buildNo)
    
    let branch = environVar "APPVEYOR_REPO_BRANCH"
    
    if branch <> "master" && branch <> "develop" then
        printf "Only master and develop branches are allowed to update imageserv, skipping\n"
    elif isNullOrEmpty img_repo then
        printf "No image server information present, skipping update\n"
    else
        
        if not (directoryExists img_path) then
            gitCommand "." ("clone --depth 1 --branch "+img_branch+" https://"+img_repo+" "+img_path)
        cd img_path
        gitCommand "." ("pull")
        
        WriteFile "paket.references" ["Appfail.WebForms"]
        WriteFile "paket.dependencies" ["source https://nuget.org/api/v2";
                                        "source https://www.myget.org/F/imazen-nightlies";
                                        "";
                                        "nuget Appfail.WebForms";]
        
        for nuSpec in Directory.GetFiles(rootDir + "nuget", "*.nuspec") do
            if not (nuSpec.Contains(".symbols.nuspec")) then
                if not (nuSpec.Contains(".x64")) then
                    if not (nuSpec.Contains("Sample")) then
                        let pkg = (fileNameWithoutExt nuSpec)
                        WriteToFile true "paket.dependencies" ["nuget " + pkg + " " + ver]
                        WriteToFile true "paket.references" [pkg]
        
        Shell.Exec (fixSlashes(".paket\\paket.bootstrapper.exe")) |> ignore
        Shell.Exec (fixSlashes(".paket\\paket.exe"), "update --redirects --force") |> ignore
        
        gitCommand "." ("add .")
        gitCommand "." ("commit -m \"AutoCommit: CI build "+ver+"\"")
        gitCommand "." ("push")
        
        cd ".."
)

Target "pack" (fun _ ->
    Run "pack_nuget"
    Run "pack_zips"
)

Target "push" (fun _ ->
    Run "push_nuget"
    Run "push_zips"
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
        printf "\nLarge files in %s %dk:\n" (Path.GetFileName(zipPkg)) (FileInfo(zipPkg).Length / 1024L)
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
    let cliVersionString = ref ""
    
    let filterList = 
      fun x -> 
          let parts = (split ' ' x)
          if parts.[0] = "release" then
              cliVersionString := parts.[1]
          parts.[0]

    let targetList = List.map(filterList)  (split ';' targets)

    let user_is_pushing = targets.Contains "push" || targets.Contains "do_all"

    if user_is_pushing && isRelease && (not isAutoBuild) && releaseVersionString <> !cliVersionString then
        if !cliVersionString = "" then
            failwith "Error: pushing of releases disabled from cli. To continue add 'release <semver>' to the target list that matches git tag."
        else
            failwith (sprintf "Error: git tag doesn't match cli release input (git: %s, cli: %s)" releaseVersionString !cliVersionString)
    
    for target in List.filter(fun name -> name <> "release") targetList do
        Run target
)

Target "do_all" (fun _ ->
    "clean"
    ==> "patch_info"
    ==> "build"
    ==> "test"
    ==> "unmess"
    ==> "pack"
    ==> "push"
    ==> "update_imageserv"
    ==> "print_stats"
    
    
    Run "print_stats"
)

Target "do_anonymous" (fun _ ->
    "clean"
    ==> "patch_info"
    ==> "build"
    ==> "test"
    ==> "unmess"
    ==> "pack"
    ==> "print_stats"
    Run "print_stats"
)


RunTargetOrDefault "do_all"
