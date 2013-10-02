module ImageResizer.TestUtils

open System.Resources
open System.Reflection

let get800x600 ()=
        Assembly.GetExecutingAssembly().GetManifestResourceStream("ImageResizer.TestFSharp.800x600white.png")

