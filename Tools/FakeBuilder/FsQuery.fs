module Fake.FsQuery

open System
open System.IO
open System.Text.RegularExpressions

type PathPattern(pattern: string) = 
    inherit Regex(pattern.Replace("/","\\\\").Replace(".","\\.").Replace("*",".*"), RegexOptions.Compiled ||| RegexOptions.CultureInvariant ||| RegexOptions.ExplicitCapture ||| RegexOptions.IgnoreCase ||| RegexOptions.Singleline)


let toPatterns strings=
   strings |> Seq.toList |> List.map(fun x -> new PathPattern(x))
let tupleRelative baseDir (pathStrings:list<string>) =
    pathStrings |> 
    List.filter(fun item -> item.StartsWith(baseDir)) |>
    List.map(fun item -> (item, item.Substring(baseDir.Length).TrimEnd('\\', '/')))

type FsInventory(folder :string) =
  let mutable iFiles = []
  member this.baseDir = folder.TrimEnd('\\','/')
  member this.files
    with get () =
      if iFiles = [] then Directory.GetFiles(this.baseDir, "*", SearchOption.AllDirectories) |>  Array.toList |> tupleRelative this.baseDir
      else iFiles
    and set (value) = iFiles <- value
  member this.folders = Directory.GetDirectories(this.baseDir, "*", SearchOption.AllDirectories) |>  Array.toList |> tupleRelative(this.baseDir)

type FsQuery(inventory:FsInventory, exclusions: list<PathPattern>) = 
  let inv = inventory
  let excl = exclusions
  let query(baseDir:string, paths:list<string * string>, exclusions:list<PathPattern>, queries:list<PathPattern>)=
    paths |> List.filter 
      (fun (f,r) -> 
           (exclusions |> List.tryFind(fun e -> e.IsMatch(r)) = None) &&
           (queries |> List.tryFind(fun e -> e.IsMatch(r)) <> None)) 
          |> List.map (fun (f,r) -> f)

  
  member this.exclude([<ParamArray>] exclusions: string list) = 
    new FsQuery(inv,excl @ toPatterns exclusions)
  
  member this.exclude([<ParamArray>] exclusions: string[]) = 
    new FsQuery(inv,excl @ toPatterns exclusions)
  
  member this.exclude(exclusions: list<PathPattern>) = 
    new FsQuery(inv,excl @ exclusions)
  
  member this.files([<ParamArray>] queries: string list) =
    query(inv.baseDir,inv.files,excl,toPatterns queries)
  
  member this.files([<ParamArray>] queries: string[]) =
    query(inv.baseDir,inv.files,excl,toPatterns queries)
  
  member this.files([<ParamArray>] queries: PathPattern[]) =
    query(inv.baseDir,inv.files,excl, queries |> Array.toList)

  member this.folders([<ParamArray>] queries: string[]) =
    query(inv.baseDir,inv.folders,excl,toPatterns queries)
 
  member this.folders([<ParamArray>] queries: PathPattern[]) =
    query(inv.baseDir,inv.folders,excl, queries |> Array.toList)