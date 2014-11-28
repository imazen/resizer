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



let fb_nuget_url = EnvironmentHelper.environVar "fb_nuget_url"
let fb_nuget_key = EnvironmentHelper.environVar "fb_nuget_key"

// Nuspec

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
    
    let publish =
        if fb_nuget_url = null then false
        elif fb_nuget_url = "" then false
        else true
    
    for nuSpec in Directory.GetFiles(rootDir + "nuget", "*.nuspec") do
        replaceNuspec nuSpec (rootDir+"Releases/nuspec-tmp/"+Path.GetFileName(nuSpec))
    
    for nuSpec in Directory.GetFiles(rootDir + "Releases/nuspec-tmp", "*.nuspec") do
        if not (nuSpec.Contains(".symbols.nuspec")) then
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
)

Target "Deploy" (fun _ ->
    Run "NuDeploy"
)

"CleanAll"
==> "PatchInfo"
==> "BuildAll"
==> "Deploy"

RunTargetOrDefault "Deploy"
