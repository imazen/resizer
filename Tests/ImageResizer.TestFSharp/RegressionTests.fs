
namespace ImageResizer.TestFSharp

open Xunit
open FsUnit.Xunit
open ImageResizer
open ImageResizer.Configuration
open ImageResizer.TestUtils
open System.Drawing

type ``Given a 800x600 bitmap`` ()=
  let b = get800x600()
  let c = new Config()

  [<Fact>] 
  member this.``manual cropping should correctly affect the final size of the image``() =
      let i = new Instructions("cropxunits=100;cropyunits=100;crop=10,30,90,60")
      let result = c.CurrentImageBuilder.Build(new ImageJob(b, typedefof<Bitmap>, i)).Result :?> Bitmap
      result.Size |> should equal (new Size(640,180))

  [<Fact>] 
  member this.``manual cropping should correctly affect the final size of the image when srotate=0``() =
      let i = new Instructions("cropxunits=100;cropyunits=100;crop=10,30,90,60;srotate=0")
      let result = c.CurrentImageBuilder.Build(new ImageJob(b, typedefof<Bitmap>, i)).Result :?> Bitmap
      result.Size |> should equal (new Size(640,180))

  [<Fact>] 
  member this.``manual cropping should correctly affect the final size of the image when srotate=90``() =
      let i = new Instructions("cropxunits=100;cropyunits=100;crop=10,30,90,60;srotate=90")
      let result = c.CurrentImageBuilder.Build(new ImageJob(b, typedefof<Bitmap>, i)).Result :?> Bitmap
      result.Size |> should equal (new Size(480, 240))