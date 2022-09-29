# ImageResizer.Plugins.Imageflow

This is an image processing backend that uses the [Imageflow](https://github.com/imazen/imageflow) library by yours truly.
Imageflow is written in Rust for speed and security, and offers incredible performance and file optimization.

### Installation
* Enable pre-release packages in NuGet
* `Install-Package ImageResizer.Plugins.Imageflow -pre`
* `Install-Package Imageflow.NativeRuntime.win-x86 -pre` installs the 32-bit imageflow.dll
* `Install-Package Imageflow.NativeRuntime.win-x86_64 -pre` installs the 64-bit imageflow.dll
* Add `<add name="Imageflow" />` to the `<resizer><plugins>` section of `Web.config` to use Imageflow.
* The functionality from most plugins has been integrated into the core or into Imageflow, drastically simplifying maintenance for the most common features.
* Remove references to `Wic*`, `PrettyGifs`, `AnimatedGifs`, `Watermark`, `SimpleFilters`, `WhitespaceTrimmer`, `WebP`, `AdvancedFilters`, `FastScaling.x86` and `FastScaling.x64` from **both nuget.config and Web.config**.
* Note that Imageflow does not yet support .TIFF files. The default pipeline will be used for these, so advanced file editing and compression will not be available.


### Images are rotated using metadata

Images are now rotated, by default, based on EXIF metadata from the gravity sensor in your camera/phone. We've suggested setting `<pipeline defaultCommands="autorotate.default=true"/>` for five years now, so this may not affect you.

The Imageflow backend does not support &autorotate=false.

### Most AdvancedFilters commands are gone (it was an alpha plugin)

* Imageflow now implements `a.balancewhite`.
* Sharpening is now done with `f.sharpen` & Imageflow, not `a.sharpen`, and `a.sharpen` is ignored. We don't map the command since they produce different results.
* Blurring and noise removal are not yet supported, so `a.removenoise` and `a.blur` are ignored.
* `a.oilpainting`, `a.sobel`,  `a.threshold`, `a.canny`, `a.equalize`, `a.posterize` are gone.

### Imageflow will not be used in niche cases
The default GDI pipeline will be used (thus disabling file optimization, filters, and other Imageflow features) under the following conditions:
* `&rotate` values other than 0, 90, 180, 270 (or another multiple of 90) are used.
* The input file has a .tiff, .tff, .tif, or .bmp extension (Imageflow does not support TIFF and BMP formats, yet).
* `&frame=x` is used to select a frame from an animated GIF.
* `paddingWidth`, `paddingHeight`, `margin`, `borderWidth`, `borderColor` or `paddingColor` are used (obsolete, we have good CSS now). `&bgcolor=AARRGGBB` is still supported, of course.


### Imageflow supports nearly everything:
`mode`, `anchor`, `flip`, `sflip`,
`quality`, `zoom`, `dpr`, `crop`, `cropxunits`, `cropyunits`,
`w`, `h`, `width`, `height`, `maxwidth`, `maxheight`, `format`,
`srotate`, `rotate`, `stretch`, `webp.lossless`, `webp.quality`,
`f.sharpen`, `f.sharpen_when`, `down.colorspace`, `bgcolor`,
`jpeg_idct_downscale_linear`, `watermark`, `s.invert`, `s.sepia`,
`s.grayscale`, `s.alpha`, `s.brightness`, `s.contrast`, `s.saturation`,
`trim.threshold`, `trim.percentpadding`, `a.balancewhite`,  `jpeg.progressive`,
`decoder.min_precise_scaling_ratio`, `scale`, `preset`, `s.roundcorners`, `ignoreicc`
