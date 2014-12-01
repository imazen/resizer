#r @"..\..\packages\FAKE\tools\FakeLib.dll"

open Fake
open System
open System.IO
open System.Text
open System.Text.RegularExpressions


let rootDir = "../"
let coreDir = rootDir + "Core/"
let mainSolution = coreDir + "ImageResizer.sln"
let assemblyInfoFile = coreDir + "SharedAssemblyInfo.cs"



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



// Nuget

let fb_nuget_url = EnvironmentHelper.environVar "fb_nuget_url"
let fb_nuget_key = EnvironmentHelper.environVar "fb_nuget_key"

let nvc = ["author", "Nathanael Jones, Imazen";
           "owners", "nathanaeljones, imazen";
           "pluginsdlldir", @"..\dlls\trial";
           "coredlldir", @"..\dlls\release";
           "iconurl", "http://imageresizing.net/images/logos/ImageIconPSD100.png";
           "plugins", "## 30+ plugins available\n\n" + 
               "Search 'ImageResizer' on nuget.org, or visit imageresizing.net to see 40+ plugins, including WPF, WIC, FreeImage, OpenCV, AForge &amp; Ghostscript (PDF) integrations. " + 
               "You'll also find  plugins for disk caching, memory caching, Microsoft SQL blob support, Amazon CloudFront, S3, Azure Blob Storage, MongoDB GridFS, automatic whitespace trimming, " +
               "automatic white balance, octree 8-bit gif/png quantization and transparency dithering, animated gif resizing, watermark &amp; text overlay support, content aware image resizing /" + 
               " seam carving (based on CAIR), grayscale, sepia, histogram, alpha, contrast, saturation, brightness, hue, Guassian blur, noise removal, and smart sharpen filters, psd editing &amp; " +
               "rendering, raw (CR2, NEF, DNG, etc.) file exposure, .webp (weppy) support, image batch processing &amp; compression into .zip archives, red eye auto-correction,  face detection, and " + 
               "secure (signed!) remote HTTP image processing. Most datastore plugins support the Virtual Path Provider system, and can be used for non-image files as well.\n\n" ]

let replaceNuspec (file:string) (savefile:string) =
    let mutable fileContents = File.ReadAllText(file, UTF8Encoding.UTF8)
    for (key, value) in nvc do
        fileContents <- Regex.Replace(fileContents, "\\$"+key+"\\$", value)
    File.WriteAllText(savefile, fileContents, UTF8Encoding.UTF8)

let nuPack nuSpec =
    let publish =
        if fb_nuget_url = null then false
        elif fb_nuget_url = "" then false
        else true
    
    NuGet (fun p -> 
      {p with
          PublishUrl = fb_nuget_url
          AccessKey = fb_nuget_key
          Publish = publish
          OutputPath = rootDir + "Releases/nuget-packages"
          WorkingDir = rootDir + "nuget"
          Project = Path.GetFileNameWithoutExtension(nuSpec)
          Version = version })
          nuSpec



// S3 packing
(*
let excludes =
    !! (rootDir + "**/.git/**")
    ++ (rootDir + "**/Hidden/**")
    ++ (rootDir + "Releases/**")
    ++ (rootDir + "Legacy/**")
    ++ (rootDir + "Tools/Builder/**")
    ++ (rootDir + "Tools/BuildTools/**")
    ++ (rootDir + "Tools/docu/**")
    ++ (rootDir + "Tools/AutoBuilder/**")
    ++ (rootDir + "Tools/FakeBuilder/**")
    ++ (rootDir + "submodules/docu/**")
    ++ (rootDir + "Samples/Images/extra/**")
    ++ (rootDir + "Samples/Images/private/**")
    ++ (rootDir + "Samples/MvcSample/App_Data/**")
    ++ (rootDir + "**/Thumbs.db")
    ++ (rootDir + "**/.DS_Store")
    ++ (rootDir + "**/*.suo")
    ++ (rootDir + "**/*.cache")
    ++ (rootDir + "**/*.user")
*)

let packMin =
    !! (rootDir + "dlls/release/ImageResizer.???")
    ++ (rootDir + "readme.txt")
    ++ (rootDir + "Core/license.txt")
    ++ (rootDir + "Web.config")

let packAllBinaries =
    !! (rootDir + "dlls/release/*.dll")
    ++ (rootDir + "dlls/release/*.pdb")
    ++ (rootDir + "*.txt")

let packStandard =
    !! (rootDir + "dlls/release/ImageResizer.???")
    ++ (rootDir + "dlls/debug/**")
    ++ (rootDir + "dlls/release/**")
    ++ (rootDir + "submodules/studiojs/**")
    ++ (rootDir + "core/**")
    ++ (rootDir + "samples/**")
    ++ (rootDir + "*.txt")
    ++ (rootDir + "Web.config")
    -- (rootDir + "**/.git/**")
    -- (rootDir + "Samples/Images/extra/**")
    -- (rootDir + "Samples/Images/private/**")
    -- (rootDir + "Samples/MvcSample/App_Data/**")
    -- (rootDir + "**/Thumbs.db")
    -- (rootDir + "**/.DS_Store")
    -- (rootDir + "**/*.suo")
    -- (rootDir + "**/*.cache")
    -- (rootDir + "**/*.user")

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

Target "CleanAll" (fun _ ->
    MSBuild "" "Clean" ["Configuration","Release"] [mainSolution] |> ignore
    MSBuild "" "Clean" ["Configuration","Debug"] [mainSolution] |> ignore
    MSBuild "" "Clean" ["Configuration","Trial"] [mainSolution] |> ignore
    CleanDirs [rootDir + "dlls/release"]
    CleanDirs [rootDir + "dlls/debug"]
    CleanDirs [rootDir + "dlls/trial"]
)

Target "BuildAll" (fun _ ->
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

Target "NuDeploy" (fun _ ->
    CleanDirs [rootDir + "Releases/nuspec-tmp"]
    CleanDirs [rootDir + "Releases/nuget-packages"]
    
    // replace nuget vars
    for nuSpec in Directory.GetFiles(rootDir + "nuget", "*.nuspec") do
        replaceNuspec nuSpec (rootDir+"Releases/nuspec-tmp/"+Path.GetFileName(nuSpec))
    
    // process symbol packages first
    for nuSpec in Directory.GetFiles(rootDir + "Releases/nuspec-tmp", "*.symbols.nuspec") do
        nuPack nuSpec
        let baseName = rootDir + "Releases/nuget-packages/" + Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(nuSpec)) + "." + version
        File.Move(baseName + ".nupkg", baseName + ".symbols.nupkg")
    
    // process regular packages
    for nuSpec in Directory.GetFiles(rootDir + "Releases/nuspec-tmp", "*.nuspec") do
        if not (nuSpec.Contains(".symbols.nuspec")) then
            nuPack nuSpec
)

Target "S3Deploy" (fun _ ->
    for file in deletableFiles do
        DeleteFile file
    
    let mutable zipable = []
    
    for file in packStandard do
        zipable <- List.append zipable [file]
    
    Zip rootDir (rootDir + "standard-test.zip") (List.toSeq zipable)
)

Target "Deploy" (fun _ ->
    Run "NuDeploy"
    //Run "S3Deploy"
)

"CleanAll"
==> "PatchInfo"
==> "BuildAll"
==> "Deploy"

RunTargetOrDefault "Deploy"
