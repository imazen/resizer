Tags: plugin
Edition: performance
Tagline: "FastScaling offers the highest-quality image scaling and sharpening available. It can also be up to 43x faster than Graphics.DrawImage, the default."
Aliases: /plugins/fastscaling

# FastScaling plugin

Unlike the FreeImage and WIC pipelines, FastScaling integrates with the default GDI+ pipeline, replacing Graphics.DrawImage() with its own algorithms. 
This [bypasses the severe performance and moderate quality issues of Graphics.DrawImage](#drawimage), while allowing existing plugins and System.Drawing-dependent code to function as-is.
 

FastScaling uses best-in-class resampling algorithms, and can perform them in the right 'working' color space (or in a color space customized for the shadow/highlight preservation needs of your image).

Unlike DrawImage, it uses orthogonal/separable resampling, and requires less of the CPU cache. On a 4-core laptop, **this can translate into a 16-30X increase in throughput** when benchmarked against DrawImage for photos from your average 16MP pocket camera. Even when executed on a single-thread, this can mean a 5-12X performance advantage. On a Azure D14 instance, FastScaling has been measured to be 43x faster).

Will you always see benefits this drastic? No. For tiny images that can fit - in their entirety - on your CPU cache (say 300x300), the gain is small - 25% on a single thread, or 0.8 x core count for a throughput increase.

If you have a large number of cores, you might see much *larger* increases in throughput.

Including the jpeg encoding and decoding cost (which FastScaling does not address), enabling FastScaling will translate into roughly a 4.5-9.6X increase in overall performance for 16MP images.


## Benchmarks

The following benchmarks were created by opening the FastScaling solution on [commit de24b168](https://github.com/imazen/resizer/commit/de24b168457279da74202676e4be4eccad1b6b53) and running the Benchmark program in 64-bit Release mode, using an Azure D14 instance with Visual Studio 2015 RC installed. Source images are 4,000 x 4,000 jpeg images, and destination images are 800x800 jpegs.

This benchmark measures only rendering performance (it excludes jpeg encoding/decoding, which is not affected by FastScaling). Relative performance peaks at 43.5x faster (on 15 threads).

![FastScaling vs DrawImage (Azure D14 instance)](http://z.zr.io/rw/scaling-benchmark.png?crop=30,40,-1,-1&width=700)

This benchmark measures end-to-end image decoding, rendering, and re-encoding. It excludes I/O. Relative performance peaks at 9.6x faster (on 8 threads).

![FastScaling vs DrawImage (including decode and encode) (Azure D14 instance)](http://z.zr.io/rw/decode-scale-encode.png?crop=30,0,-1,-1&width=700)

The raw [CSV results are here](https://github.com/imazen/Graphics-vNext/blob/master/fastscaling-benchmark-azure-d14.csv), and include a performance delta row. 

## Usage (From InternalDrawImage, BuildJob, or the URL API )

**FastScaling can be activated per-request using `&fastscale=true` or site wide by specifying `fastscale=true` within the `<pipeline defaultCommands=""/>` attribute.**

Upscaling and downscaling may utilize the same algorithm, but should be tuned quite differently. Fastscaling's command structure reflects this. All &down.* commands have a &up. equivalent. 

* `&down.filter=Robidoux`
* `&up.filter=Ginseng`

The default downscaling filter is Robidoux, and the default upscaling filter is Ginseng. To mimic DrawImage precisely, use Cubic

### Resampling filters

Filters may be removed or renamed prior to release. You are reminded that FastScaling is still alphaware.

* Robidoux
* RobidouxSharp
* RobidouxFast
* Ginseng
* GinsengSharp
* Lanczos
* LanczosSharp
* Lanczos2
* Lanczos2Sharp
* CubicFast
* Cubic
* CubicSharp
* CatmullRom
* Mitchell
* Fastest

### Sharpening

FastScaling can typically perform sharpening at zero extra cost, as the sharpening percentage is composed into the weighting function used for resampling.
 For no-op scaling or certain (small) 'Fast' filters, this adds a tiny amount of overhead. FastScaling sharpening is roughly 20x faster than the sharpening provided by AdvancedFilters.

* `&f.sharpen=0..100`

### Why colorspaces matter

Another failing of DrawImage is that it only averages pixels in the sRGB color space. sRGB is a perceptual color space, meaning that fewer numbers are assigned to bright colors; most are assigned to shades of black. When downscaling (weighted averaging), this tends to exaggerate shadows and make highlights disappear, although it is just fine when upscaling.

FastScaling defaults to working in the srgb color space too - but only because users expect DrawImage-like behavior, not because sRGB is better. Linear light is almost always a better choice for downscaling than sRGB, and is more 'correct'.

We expose 2 ways to control the 'working color space'.

* `&down.preserve=-10..10`  - Generates a color space that will favor preservation of shadows (negative values) or highlights(positive values). 0 represents linear light.

When upscaling, sRGB produces good results. `&up.colorspace=srgb` is the default.

Fast

* `&down.colorspace=srgb (as-is, default) | linear| gamma`  `srgb` is equivalent to `&down.preserve=-6.1515`, and mirrors DrawImage behavior. `linear` is equivalent to `&down.preserve=0`
* `&down.colorspace.a=1..2.2` lets you set a (temporary) gamma adjustment directly. Use with `&down.colorspace=gamma`.

As you can tell, scaling in a linear light (left) preserves more of the snowflakes than scaling in sRGB (right).
![Scaling in linear](https://s3.amazonaws.com/resizer-web/pluginexamples/snowing_300_linear.jpg)
![Scaling in sRGB](https://s3.amazonaws.com/resizer-web/pluginexamples/snowing_300_srgb.jpg)


## Advanced optimizations

FastScaling can make a range of adjustments to favor speed or quality on very large images. These typically set default values for the filter type/size and for averaging optimizations. 

Averaging optimization: If you're downscaling an image to 1/20th of its size, FastScaling will use an averaging filter to scale it to 1/6th, then scale the remaining 1/3.333 using a high-quality filter. Since no filters use a window larger than ~3x the scale filter, this does not measurably affect quality. 
You can make this optimization more aggressive by increasing the speed value, or reduce it by specifying a negative value. 

* `&down.speed = -2..4` (default 0)  -2 disables averaging optimizations. 
* `&up.speed = 0..2` (default 0)

If you know that transparency information can be discarded, specify `&f.ignorealpha=true`. This will prevent FastScaling from taking the slow compositing path when working with two 32-bit images.

<a name="drawimage"></a>

## Performance problems with DrawImage

[Graphics.DrawImage()](https://msdn.microsoft.com/en-us/library/system.drawing.graphics.drawimage%28v=vs.110%29.aspx) holds a process-wide lock, and is a very severe bottleneck for any imaging work on the GDI+/.NET platform. 
This is unfortunate, as WIC and WPF do not offer any high-quality resampling filters, and DirectX is 10-20X slower than DrawImage. 

DrawImage also implements a general distortion filter. This type of filter thrashes the CPU cache; it is not optimized for linear memory access. It does not parallelize well on multiple cores even when used in separate processes.


## Blurring and sharpening the *wrong* way

FastScaling exposes 2 'advanced-only' parameters that can allow you to blur, sharpen, or 'artifact' your image in very messy ways. These interact with how (and how many) pixels are weighted and averaged. 

* `&down.blur=0.5..2` Default: 1. Values smaller than 1 will create a psuedo-sharpening effect, by interpreting input pixels as being closer to the support window center than they actually are. Positive values do the opposite, and create an inaccurate blur effect.
* `&down.window=0.5..3` Default 1..3, depends on filter. This describes the support window (input pixel set) size relative to the output pixel's corresponding input area. Values of 1 will involve only corresponding pixels. Values of 2 will involve the corresponding area, plus half again on each side. Values of three will triple the number of input pixels. True sharpening requires values above ~1.6, as sharpening requires negative weighting of neighboring pixels. FastScaling will switch to independent sharpening if the window is not large enough. 

up.blur, down.blur, up.window, and down.window may be disabled in future versions of FastScaling. Different filters have different defaults for these values, so it's advisable to use `&down.filter`, `&down.speed`, and `&f.sharpen` instead of these.  



## Caveats to Fastscaling as a DrawImage replacement

There are certain features of DrawImage that FastScaling does not replace. You are advised to fall back to DrawImage usage if you:

* Need to rotate or skew the image by an arbitrary number of degrees. FastScaling only supports 90-degree interval rotation and flipping. Arbitrary cropping, scaling and compositing is supported.
* Want to apply ImageAttributes other than colorMatrix or SetWrapMode - FastScaling does the right thing with border pixels automatically.

Note that if you specify a colorMatrix, FastScaling will do all work in the srgb space instead of in linear light. This is required for the colorMatrix values to be interpreted in a manner consistent with DrawImage.

If you are working with a format other than 32-bit BGRA, 32-bit BGR, or 24-bit BGR, you should first convert to BGR or BGRA before using FastScaling. It is still faster to convert to/from BGR(A) and use FastScaling than it is to use DrawImage directly.

## Installation

FastScaling is a self-contained mixed-mode DLL. You will need to install the correct NuGet package depending upon whether your application is run as a 32-bit program or as a 64-bit program. IIS Express can run in either mode, and different installations of Visual Studio can have different results. 

You will also need into install the [Visual C++ 2013 Redistributable package](https://www.microsoft.com/en-us/download/details.aspx?id=40784) if it is not already installed. 

`Install-Package ImageResizer.Plugins.FastScaling.x64`

or

`Install-Package ImageResizer.Plugins.FastScaling.x86`

If FastScaling has not yet been published to nuget.org, you will need to use our nightly feed, `https://www.myget.org/F/imazen-nightlies/api/v2`.

After installing the nuget package or dll, you will need to install the plugin. You may also want to enable FastScaling for all requests, instead of per-request, via `defaultCommands="fastscale=true"`. 

```
<resizer>
  <pipeline defaultCommands="fastscale=true" />
  <plugins>
    <add name="FastScaling" />
  </plugins>
</resizer>
```

If you are installing via code instead of XML, call `new ImageResizer.Plugins.FastScaling.FastScalingPlugin().Install(Config.Current);` during application startup. You will need to explicitly specify `&fastscale=true` on every image job or request in order to activate the plugin.






