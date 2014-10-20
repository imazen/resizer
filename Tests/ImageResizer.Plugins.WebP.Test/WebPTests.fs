namespace ImageResizer.Plugins.WebP.Test

open Xunit
open FsUnit.Xunit
open ImageResizer
open ImageResizer.Configuration
open ImageResizer.Plugins.WebPDecoder
open ImageResizer.Plugins.WebPEncoder
open System.IO

type ``Test WebP`` ()=
  let c = new Config()
  let enc = new WebPEncoderPlugin()
  let dec = new WebPDecoderPlugin()
  let grad = new ImageResizer.Plugins.Basic.Gradient()
  do
    c.Plugins.LoadNativeDependenciesForType(typeof<ImageResizer.Plugins.WebPDecoder.WebPDecoderPlugin>)
    enc.Install(c) |> ignore
    dec.Install(c) |> ignore
    grad.Install(c)  |> ignore

  [<Fact>] 
  member test.``We can encode a webp image``() =
    let ms = new MemoryStream()
    let job = new ImageJob("~/gradient.png", ms, new Instructions("width=100&height=100&format=webp"))
    c.CurrentImageBuilder.Build(job) |> ignore


  [<Fact>] 
  member test.``We can encode and decode a webp image``() =
    let ms = new MemoryStream()
    let ms2 = new MemoryStream()
    let job = new ImageJob("~/gradient.png", ms, new Instructions("width=100&height=100&format=webp"))
    c.CurrentImageBuilder.Build(job) |> ignore
    ms.Seek(0L,SeekOrigin.Begin) |> ignore
    let job2 = new ImageJob(ms, ms2, new Instructions("width=50&format=jpg"))
    c.CurrentImageBuilder.Build(job2) |> ignore




