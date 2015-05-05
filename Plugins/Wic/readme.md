Tags: plugin
Edition: creative
Tagline: "3 plugins:  WicDecoder supports decoding images through WIC, supporting any image codecs installed on the computer. WicEncoder encodes jpeg, gif, and png images through WIC for better performance and more control. Adjust jpeg quality, subsampling, gif dithering, and palette size. WicBuilder provides a completely alternate pipeline, which supports most basic resize/crop/pad operations. 2-4x faster than the default pipeline. Enable on a per-request basis."
Aliases: /plugins/wic


# WIC Plugins (V3.1+)

ImageResizer.Plugins.Wic.dll contains 3 plugins: WicImageDecoder, WicImageEncoder, and WicImageBuilder.

These plugins require Windows 7 or Windows Server 2008 R2 for best performance and correct behavior.

Vista or Windows Server 2008 with the Platform Update applied should also work.

## Installation

1. Either run `Install-Package ImageResizer.Plugins.Wic` in the NuGet package manager, or add ImageResizer.Plugins.Wic.dll to your project
2. Add `<add name="WicDecoder" />` inside the `<plugins>` section of Web.config.
3. Add `<add name="WicEncoder" />` inside the `<plugins>` section of Web.config.
4. Add `<add name="WicBuilder" />` inside the `<plugins>` section of Web.config.


## WicDecoder

Supports decoding images through WIC, supporting any image codecs installed on the computer. When combined with a codec pack, can open RAW files. 

Activate with `&decoder=wic`. 

* page=1..?
* frame=1..?

## WicEncoder

Encode jpeg, gif, and png images through WIC for better performance and more control. Adjust jpeg quality, subsampling, gif dithering, and palette size.

Vs. PrettyGifs: 3-8x faster for encoding 8-bit PNG images. 2-5x faster for GIF images. 

Activate with `&encoder=wic`

* quality = 0..100
* subsampling=444&#124;422&#124;420
* dither=false&#124;true (default is true, unlike PrettyGifs)
* colors=2..256


## WicBuilder

Provides a completely alternate pipeline, which supports most basic resize/crop/pad operations. 

2-4x faster than the default pipeline. Slightly reduced image quality.

Activate with `&builder=wic`

Select the resizing filter with `w.filter=fant|bicubic|linear|nearest`

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
