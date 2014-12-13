module FakeBuilder.Nuget

open Fake
open System
open System.IO
open System.Text
open System.Text.RegularExpressions

let fillVariables (file:string) (savefile:string) (keyValTuples: seq<string*string>) =
    let mutable fileContents = File.ReadAllText(file, UTF8Encoding.UTF8)
    for (key, value) in keyValTuples do
        fileContents <- Regex.Replace(fileContents, "\\$"+key+"\\$", value)
    File.WriteAllText(savefile, fileContents, UTF8Encoding.UTF8)

let pack nuSpec version outDir =
    try
        let args = sprintf "pack -Version %s -OutputDirectory %s %s" version outDir nuSpec
        let result =
            ExecProcess(fun info ->
                info.FileName <- "nuget"
                info.Arguments <- args)
                (TimeSpan.FromMinutes 1.0)
        if result <> 0 then failwithf "Error during NuGet packing (%s)" args
    with exn ->
        raise exn

let push nuPkg url key =
    let args =
        if url = null || url = "" then
            (sprintf "push %s %s" nuPkg key)
        else (sprintf "push %s %s -s %s" nuPkg key url)
    
    // hide command so the api key doesn't leak
    let tracing = enableProcessTracing
    enableProcessTracing <- false
    
    let mutable tries = 3
    
    while tries > 0 do
        try
            let result =
                ExecProcess(fun info ->
                    info.FileName <- "nuget"
                    info.Arguments <- args)
                    (TimeSpan.FromMinutes 1.0)
            if result <> 0 then failwithf "Error during NuGet push (%s)" nuPkg
            else tries <- 0
        with exn ->
            tries <- tries-1
            if tries=0 then
                raise exn
    
    // restore settings
    enableProcessTracing <- tracing
