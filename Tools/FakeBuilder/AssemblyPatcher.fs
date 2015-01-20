module FakeBuilder.AssemblyPatcher

open Fake
open System
open System.IO
open System.Text
open System.Text.RegularExpressions

let replaceInfo info key value : string =
    let pattern = "(\\[assembly:\\s+" + key + "[^\]]*\\])"
    let replace = "[assembly: " + key + "(\"" + value + "\")]"
    let result = Regex.Replace(info, pattern, replace)
    // replace might have worked, but matches source
    if result.Contains(replace) then result
    else info + replace + "\n"

let getInfo file key : string =
    let info = File.ReadAllText(file, UTF8Encoding.UTF8)
    let pattern = "\\[assembly:\\s+" + key + "\\s*\\(\"([^\"]*)"
    let m = Regex.Match(info, pattern)
    if m.Success then m.Groups.[1].Value
    else ""

let setInfo file (keyValTuples: seq<string*string>) =
    let mutable info = File.ReadAllText(file, UTF8Encoding.UTF8)
    for (key, value) in keyValTuples do
        info <- replaceInfo info key value
    File.WriteAllText(file, info, UTF8Encoding.UTF8)
