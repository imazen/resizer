[<AutoOpen>]
/// This module contains helper function to create and extract zip archives.
module Fake.ZipHelper2
 
open Fake
open System.IO
open System.Text
open System.Text.RegularExpressions
open ICSharpCode.SharpZipLib.Zip
open ICSharpCode.SharpZipLib.Core
open System
 
/// The default zip level
let DefaultZipLevel = 7
 
type ZipInputFile = 
        | File of sourcePath : string
        | CustomFile of sourcePath : string * destPath : string * required : bool
        | CustomFileTransform of sourcePath : string * destPath : string * required : bool * find : string * replace : string
   
type ZipFileInfo = { sourcePath:string; destPath:string; required:bool; info:FileInfo; find:string; replace:string;}
    
 
/// Creates a zip file with the given files
/// ## Parameters
///  - `workingDir` - The relative dir of the zip files. Use this parameter to influence directory structure within zip file.
///  - `fileName` - The fileName of the resulting zip file.
///  - `comment` - A comment for the resulting zip file.
///  - `level` - The compression level.
///  - `flatten` - If set to true then all subfolders are merged into the root folder.
///  - `files` - A sequence with files to zip.
let CreateZip workingDir fileName comment level flatten (files:seq<ZipInputFile>) = 
    
    let workingDir = 
        let dir = directoryInfo workingDir
        if not dir.Exists then failwithf "Directory not found: %s" dir.FullName
        dir.FullName
 
    
    let getDestName (info:FileInfo) =
      if flatten then info.Name
      else if not (String.IsNullOrEmpty(workingDir)) 
              && info.FullName.StartsWith(workingDir, true, Globalization.CultureInfo.InvariantCulture) then 
          info.FullName.Remove(0, workingDir.Length)
      else info.FullName
   
    let getFileInfo (zif:ZipInputFile) = 
        match zif with
        | File x -> {sourcePath=x; destPath=getDestName(fileInfo x); required=false; info=(fileInfo x); find=null; replace=null}
        | CustomFile(x, destPath, required) -> {sourcePath=x; destPath=destPath; required=required; info=(fileInfo x); find=null; replace=null}
        | CustomFileTransform(x, destPath, required, find, replace) -> {sourcePath=x; destPath=destPath; required=required; info=(fileInfo x); find=find; replace=replace}
        
 
    let fileRecs = Seq.map getFileInfo files
 
    use stream = new ZipOutputStream(File.Create(fileName))
    let zipLevel = min (max 0 level) 9
    tracefn "Creating Zipfile: %s (Level: %d)" fileName zipLevel
    stream.SetLevel zipLevel
    if not (String.IsNullOrEmpty comment) then stream.SetComment comment
    let buffer = Array.create 32768 0uy
    for item in fileRecs do
        try
            if item.info.Exists then 
                
                let itemSpec = ZipEntry.CleanName item.destPath
                logfn "Adding File %s" itemSpec
                
                if item.find = null then
                    let entry = new ZipEntry(itemSpec)
                    entry.DateTime <- item.info.LastWriteTime
                    entry.Size <- item.info.Length
                    use stream2 = item.info.OpenRead()
                    stream.PutNextEntry(entry)
                    let length = ref stream2.Length
                    stream2.Seek(0L, SeekOrigin.Begin) |> ignore
                    
                    while !length > 0L do
                        let count = stream2.Read(buffer, 0, buffer.Length)
                        stream.Write(buffer, 0, count)
                        length := !length - (int64 count)
                
                else
                    printf "Applying regex transformations to %s\n" (Path.GetFileName(item.sourcePath))
                    use stream2 = item.info.OpenText()
                    let contents = stream2.ReadToEnd()
                    let transform = Regex.Replace(contents, item.find, item.replace, RegexOptions.Singleline)
                    let entry = new ZipEntry(itemSpec)
                    entry.DateTime <- item.info.LastWriteTime
                    entry.Size <- int64 transform.Length
                    stream.PutNextEntry(entry)
                    stream.Write(Encoding.ASCII.GetBytes(transform), 0, transform.Length)
            
            elif item.required then
                failwithf "CreateZip: could not find required file %s to create %s" item.sourcePath fileName
        
        with exn ->
            raise exn
    
    stream.Finish()
    tracefn "Zip successfully created %s" fileName
 
/// Creates a zip file with the given files.
/// ## Parameters
///  - `workingDir` - The relative dir of the zip files. Use this parameter to influence directory structure within zip file.
///  - `fileName` - The file name of the resulting zip file.
///  - `files` - A sequence with files to zip.
let Zip workingDir fileName files = CreateZip workingDir fileName "" DefaultZipLevel false files
 
/// Creates a zip file with the given file.
/// ## Parameters
///  - `fileName` - The file name of the resulting zip file.
///  - `targetFileName` - The file to zip.
let ZipFile fileName targetFileName = 
    let fi = fileInfo targetFileName
    CreateZip (fi.Directory.FullName) fileName "" DefaultZipLevel false [ File(fi.FullName) ]
 
/// Unzips a file with the given file name.
/// ## Parameters
///  - `target` - The target directory.
///  - `fileName` - The file name of the zip file.
let Unzip target (fileName : string) = 
    use zipFile = new ZipFile(fileName)
    for entry in zipFile do
        match entry with
        | :? ZipEntry as zipEntry -> 
            let unzipPath = Path.Combine(target, zipEntry.Name)
            let directoryPath = Path.GetDirectoryName(unzipPath)
            // create directory if needed
            if directoryPath.Length > 0 then Directory.CreateDirectory(directoryPath) |> ignore
            // unzip the file
            let zipStream = zipFile.GetInputStream(zipEntry)
            let buffer = Array.create 32768 0uy
            if unzipPath.EndsWith "/" |> not then 
                use unzippedFileStream = File.Create(unzipPath)
                StreamUtils.Copy(zipStream, unzippedFileStream, buffer)
        | _ -> ()
 
/// Unzips a single file from the archive with the given file name.
/// ## Parameters
///  - `fileToUnzip` - The file inside the archive.
///  - `zipFileName` - The file name of the zip file.
let UnzipSingleFileInMemory fileToUnzip (zipFileName : string) = 
    use zf = new ZipFile(zipFileName)
    let ze = zf.GetEntry fileToUnzip
    if ze = null then raise <| ArgumentException(fileToUnzip, "not found in zip")
    use stream = zf.GetInputStream(ze)
    use reader = new StreamReader(stream)
    reader.ReadToEnd()
 
/// Unzips a single file from the archive with the given file name.
/// ## Parameters
///  - `predicate` - The predictae for the searched file in the archive.
///  - `zipFileName` - The file name of the zip file.
let UnzipFirstMatchingFileInMemory predicate (zipFileName : string) = 
    use zf = new ZipFile(zipFileName)
    
    let ze = 
        seq { 
            for ze in zf do
                yield ze :?> ZipEntry
        }
        |> Seq.find predicate
    
    use stream = zf.GetInputStream(ze)
    use reader = new StreamReader(stream)
    reader.ReadToEnd()
