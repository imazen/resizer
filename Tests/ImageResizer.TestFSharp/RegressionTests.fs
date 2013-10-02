
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

  [<Fact>] member test.
    ``manual cropping should correctly affect the final size of the image``() =
      let j = new ImageJob(b, new Instructions("cropxunits=100;cropyunits=100;crop=10,30,90,60"))
      let expect = new Size(640,180)
        
      //c.Build(j).Result.Size |> should equal new Size(640,180)

