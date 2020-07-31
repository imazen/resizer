Tags: plugin
Edition: creative
Tagline: "(deprecated) Alternate decoder, builder, and encoder based on the Windows API (Windows Imaging Components)"
Aliases: /plugins/wic


# WIC Plugins


### Windows Imaging Components is an operating system component maintained by Microsoft. WicBuilder is a wrapper around the underlying operating system API. **We have deprecated WIC in favor of the [FastScaling plugin](/plugins/fastscaling), which offers superior quality and is under our control.**

ImageResizer.Plugins.Wic.dll contains 3 plugins: WicImageDecoder, WicImageEncoder, and WicImageBuilder.

These plugins require Windows 7+ or Windows Server 2008 R2+ for best performance and correct behavior.

## Installation

1. Either run `Install-Package ImageResizer.Plugins.Wic` in the NuGet package manager, or add ImageResizer.Plugins.Wic.dll to your project
2. Add `<add name="WicDecoder" />` inside the `<plugins>` section of Web.config.
3. Add `<add name="WicEncoder" />` inside the `<plugins>` section of Web.config.
4. Add `<add name="WicBuilder" enableHighQualityCubic="false" />` inside the `<plugins>` section of Web.config.


## WicDecoder

Supports decoding images through WIC, supporting any image codecs installed on the computer. When combined with a codec pack, can open RAW files. 

Activate with `&decoder=wic`. 

* page=1..?
* frame=1..?

## WicEncoder

Encode JPEG, GIF, and PNG images through WIC for better performance and more control. Adjust JPEG quality, subsampling, GIF dithering, and palette size.

Vs. PrettyGifs: 3-8x faster for encoding 8-bit PNG images. 2-5x faster for GIF images. 

Activate with `&encoder=wic`

* quality = 0..100
* subsampling=444&#124;422&#124;420
* dither=false&#124;true (default is true, unlike PrettyGifs)
* colors=2..256


## WicBuilder

Provides a completely alternate pipeline, which supports most basic resize/crop/pad operations. 

WIC HighQualityCubic is at least 2x slower than FastScaling. 
WIC Fant is 2-3x faster. Fant offers truly terrible quality.

Activate with `&builder=wic`

Select the resizing filter with `w.filter=fant|bicubic|linear|nearest|highqualitycubic`

Set `enableHighQualityCubic="true"` on Windows 10 and later to access a better quality filter.
If false, `fant` will be substituted for `highqualitycubic` (the default).


### Supported settings

* page=1..?
* frame=1..?
* width
* height
* mode=max&#124;pad&#124;crop
* scale
* maxwidth
* maxheight
* crop
* bgcolor
* margin
* quality
* subsampling
* dither
* colors

## License

This set of plugins is part of the [Design](/plugins) bundle, and licensed accordingly.
