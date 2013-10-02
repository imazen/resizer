
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

  member test.getResultSize(source, instructions:Instructions) = 
    use result = c.CurrentImageBuilder.Build(new ImageJob(source, typedefof<Bitmap>, instructions)).Result :?> Bitmap
    result.Size 


  [<Fact>] 
  member test.``manual cropping should correctly affect the final size of the image``() =
      test.getResultSize(b, new Instructions("cropxunits=100;cropyunits=100;crop=10,30,90,60")) |> should equal (new Size(640,180))

  [<Fact>] 
  member test.``manual cropping should correctly affect the final size of the image when srotate=0``() =
      test.getResultSize(b, new Instructions("cropxunits=100;cropyunits=100;crop=10,30,90,60;srotate=0")) |> should equal (new Size(640,180))

  [<Fact>] 
  member test.``manual cropping should correctly affect the final size of the image when srotate=90``() =
      test.getResultSize(b, new Instructions("cropxunits=100;cropyunits=100;crop=10,30,90,60;srotate=90")) |> should equal (new Size(480, 240))


