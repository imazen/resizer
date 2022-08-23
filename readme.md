<a name="top"></a>

## [ImageResizer](http://imageresizing.net) - The Flexible Image Server
[![Build status for develop branch](https://github.com/imazen/resizer/actions/workflows/dotnet.yml/badge.svg?branch=develop)](https://github.com/imazen/resizer/actions/workflows/dotnet.yml)
 View ReadMe for [latest (develop)](https://github.com/imazen/resizer/tree/develop#top), [v2](https://github.com/imazen/resizer/tree/support/v2#top), [v3]( https://github.com/imazen/resizer/tree/support/v3#top) and [v4](https://github.com/imazen/resizer/tree/support/v4#top).

![Latest NuGet version](http://img.shields.io/nuget/vpre/ImageResizer.svg) ![Issue Count](http://img.shields.io/github/issues/imazen/resizer.svg) 

If you're looking for a .NET Core 5/6+ version, check out [Imageflow.NET Server](https://github.com/imazen/imageflow-dotnet-server)

# What is it?

* An IIS/ASP.NET HttpModule & image server. On-demand image manipulation, delivery, and optimization &mdash; with low latency &mdash; makes responsive images easy
* An image processing library optimized and secured for server-side use
* A framework and collection of 40+ plugins to accomplish most common web imaging needs. 

ImageResizer has a very simple (and powerful) URL API.

![Fit modes](http://z.zr.io/rw/diagrams/resizing-modes.png)

For more information, [check out our website](http://www.imageresizing.net). To contribute, see [CONTRIBUTING](https://github.com/imazen/resizer/blob/develop/CONTRIBUTING.md).

#### Changes

We've made some major changes in V4. Check out our [changelog](https://github.com/imazen/resizer/blob/develop/CHANGES.md) for more details.

#### License
Over half of ImageResizer's plugins are available under the Apache 2.0 license. See [license.txt for details]( https://github.com/imazen/resizer/blob/develop/license.txt).


# Table of Contents
2. [Getting Started](#getting_started)
  1. [Basic Installation Guide](#installation)
  2. [Installing a Plugin Manually](#install_plugin)
4. [Reference](#reference)
  1. [List of Plugins](#plugins_list)
  1. [Full Command Reference](#full_command)
  2. [Managed API Examples](#managed_api)
  3. [Configuration Reference](#config_reference)
4. [Nifty Tricks](#tricks)
  4. [Watermark Images Based on Folder Name or Display Size](#watermark)
  5. [Generate Thumbnails and Multiple Sizes to Disk](#thumbnails)
  6. [Convert and Resize Images as They Are Uploaded](#resize_upload)
5. [Troubleshooting](#troubleshooting)
  1. [Accessing Self-Diagnostics](#self_diagnostics)
  2. [Getting Detailed Error Messages](#error_messages)
  3. [What Might be Wrong](#troubleshooting_guide)
4. [Everything Else](#everything_else)
  1. [Licensing and Support Information](#licensing_and_contract_info)
  3. [Contact Us](#contact)

<a name="getting_started"></a>

# Getting Started

Below is a basic installation guide, although a more comprehensive one can be found [on our website](http://www.imageresizing.net/docs/install).

Starting with ImageResizer v5, you'll want to avoid nuget.packages and use the ProjectReference format instead in your projects. 

If you can't use ProjectReference, you'll have to install these in each of your final exeuctable/test/web project(s).

```
PM> Install-Package Imageflow.NativeRuntime.win-x86 -pre
PM> Install-Package Imageflow.NativeRuntime.win-x86_64 -pre
```

<a name="installation"></a>

## Basic Installation Guide

#### Install from NuGet

In V5, ImageResizer rolled the vast majority of plugin features into ImageResizer.Plugins.Imageflow. That's why so many plugins are deprecated; they're duplicating Imageflow functionality. 

Nearly all [ImageResizer plugins are on NuGet](https://www.nuget.org/packages?q=imazen). [You can enable source symbols from symbolsource.org](http://www.symbolsource.org/Public/Home/VisualStudio) for an enhanced debugging experience.

Get the basics:

```
PM> Install-Package ImageResizer.WebConfig
PM> Install-Package ImageResizer.Plugins.Imageflow
PM> Install-Package ImageResizer.Plugins.HybridCache
```

<a name="install_plugin"></a>

#### Manual Plugin Installation

1. In *your* project, add a reference to the plugin DLL (or project, if you're using the source).
2. Configure the plugin to be installed at startup via (i) or (ii). 
  1.  In the [&lt;plugins /> section](#config_reference) of Web.config, insert `<add name="PluginName" />`
  2.  In `Application_Start`, create an instance of the plugin and install it.

```c#
  new PluginName().Install(ImageResizer.Configuration.Config.Current);
```
You will need to add the appropriate namespace reference to access the plugin.

Most configuration and plugin installation issues can be resolved by checking ImageResizer’s self-diagnostics page. If your local website is running at `http://localhost:5000/`, then you should browse to `http://localhost:5000/resizer.debug.ashx` to access it. See [the Troubleshooting](#troubleshooting section) for more details. 

<a name="reference"></a>

# Reference

<a name="plugins_list"></a>

## List of Plugins

The following is a list of all plugins currently available on ImageResizer, and links to their more detailed documentation on our website. They are grouped according to the license necessary to access them. **Higher level licenses include all plugins from lower level licenses.** Their order, from lowest to highest, is Essential, Performance, Creative, Elite. If you have any further questions about ImageResizer licenses, we encourage you to look at our [licenses page](http://www.imageresizing.net/licenses).

#### Elite License Plugins

* [CropAround plugin](http://www.imageresizing.net/plugins/croparound) - Automatic cropping based on a set of areas to preserve specified areas.
* [BatchZipper plugin](http://www.imageresizing.net/plugins/batchzipper) - Asynchronously generated .zip archives of files and resized images. Great for providing customized downloads to customers.
* [Faces plugin](http://www.imageresizing.net/plugins/faces) - Provides face detection.
* [FFmpeg plugin](http://www.imageresizing.net/plugins/ffmpeg) - Extract frames from videos by time or percentage. Includes basic blank frame avoidance. Based on ffmpeg.
* [MongoReader plugin](http://www.imageresizing.net/plugins/mongoreader) - Allows GridFS files to be resized and served.
* [PsdComposer](http://www.imageresizing.net/plugins/psdcomposer) - Dynamically edit and render PSD files - turn layers on and off, edit text layer contents, and apply certain effects.
* [RedEye plugin](http://www.imageresizing.net/plugins/redeye) - Provides sophisticated eye detection and red eye correction.
* [WebP plugins](http://www.imageresizing.net/plugins/webp) - Decode and encode .webp images.

#### Creative License Plugins

* [AdvancedFilters plugin](http://www.imageresizing.net/plugins/advancedfilters) - Blur, sharpen, remove noise, and perform automatic histogram adjustment, plus several other cool effects.
* [CopyMetadata plugin](http://www.imageresizing.net/plugins/copymetadata) - Preserve metadata during image processing.
* [WIC plugins](http://www.imageresizing.net/plugins/wic) (V3.1+)- 3 plugins: **WicDecoder** supports decoding images through WIC, and supports any image codecs installed on the computer. **WicEncoder** encodes jpeg, gif, and png images through WIC for better performance and more control. Adjust jpeg quality, subsampling, gif dithering, and palette size. **WicBuilder** provides a completely alternate pipeline, which supports most basic resize/crop/pad operations. 2-4x faster than the default pipeline. Enable on a per-request bass.
* [FreeImage plugins](http://www.imageresizing.net/plugins/freeimage) - Includes 4 plugins: **FreeImageDecoder** adds support for RAW & HDR source images - over 20 formats supported. **FreeImageEcoder** provides 2-4x Faster jpeg encoding. **FreeImageResizer** adds support for Lanczos3 and CatmullRom scaling. **FreeImageBuilder** provides an alternate imaging pipeline.
* [SeamCarving plugin](http://www.imageresizing.net/plugins/seamcarving) - Content-aware image resizing (Fast C++ exe, requires Full Trust).
* [SimpleFilters plugin](http://www.imageresizing.net/plugins/simplefilters) - Adjust photo transparency, brightness, and apply sepia and B&amp;W filters through the querystring. Nearly zero performance overhead - matrix-based.
* [Watermark plugin](http://www.imageresizing.net/plugins/watermark)(v3.1+) - Render multiple image & text overlays and background layers with incredible flexibility and great performance.
* [WhitespaceTrimmer plugin](http://www.imageresizing.net/plugins/whitespacetrimmer) - Automatically trims whitespace off an image using smart edge detection.


#### Performance License Plugins

* [AnimatedGifs plugin](http://www.imageresizing.net/plugins/animatedgifs) - Process and resize GIF animations like normal GIF images. Every frame is processed and re-encoded.
* [AzureReader2 plugin](http://www.imageresizing.net/plugins/azurereader2) - Allows blobstore images to be resized and served. Azure 2.0 compatible.
* [CloudFront plugin](http://www.imageresizing.net/plugins/cloudfront) - Allows you to use Amazon CloudFront with the resizer. Highly recommended - offers inexpensive worldwide edge caching and great scalability.
* [DiskCache](http://www.imageresizing.net/plugins/diskcache) plugin - Makes dynamic image processing as responsive and scalable as static images - because they are! Suggested, nay, *required* for websites with significant traffic. Medium-trust compatible.
* [S3Reader2 plugin](http://www.imageresizing.net/plugins/s3reader2) - Process and resize images located on a remote Amazon S3 bucket. A VirtualPathProvider. Works best when combined with DiskCache.
* [SQLReader plugin](http://www.imageresizing.net/plugins/sqlreader) - Process and resize images located in a MS SQL database. Extremely configurable, can work with nearly any database schema. A VirtualPathProvider.
* [PrettyGifs plugin](http://www.imageresizing.net/plugins/prettygifs) - Get rid of ugly .NET GIFs, and get photoshop-like results for GIFs and 8-bit PNG images. Uses tuned octree quantization and smart, adjustable dithering.
* [RemoteReader plugin](http://www.imageresizing.net/plugins/remotereader) - Allows images located on external servers to be securely resized and processed as if they existed locally.
* [FastScaling plugin](http://www.imageresizing.net/plugins/fastscaling) - Offers up to 30x faster image resizing, and much higher quality.

#### Essential License Plugin

* [ClientCache plugin](http://www.imageresizing.net/plugins/clientcache) - (default) - Sets Cache-control, Expires, and Last-modified headers for optimum performance.
* [Custom Overlay plugin](http://www.imageresizing.net/plugins/customoverlay) - **Example plugin. It is not subject to the same standards of maintenance and backwards-compatibility that normal plugins are.** This plugin is for drawing image overlays using pre-determined x1,y1,x2,y2,x3,y3,x4,y4 coordinates that are relative to the base file's width and height
* [DefaultEncoder](http://www.imageresizing.net/plugins/defaultencoder) - (default) Adjustable jpeg compression, 24-bit PNG with transparency support, and standard .NET GIF encoding (which is very lousy).
* [DefaultSettings plugin (V3.1+)](http://www.imageresizing.net/plugins/defaultsettings) - Allows you to configure the default setting values when commands (like `scale`) are omitted.
* DiagnosticJson plugin - Returns json information about the image layout.
* [Diagnostics plugin](http://www.imageresizing.net/plugins/diagnostics) - (default) - Whenever you have an issue, go to localhost/resizer.debug and you'll probably be told what is wrong.
* [Drop Shadow plugin](http://www.imageresizing.net/plugins/dropshadow) - Adds drop-shadow feature (shadowOffset, shadowWidth, shadowColor).
* [FolderResizeSyntax plugin](http://www.imageresizing.net/plugins/folderresizesyntax) - Resize images without using the query string.
* [Gradient plugin](http://www.imageresizing.net/plugins/gradient) - Create gradients from css, js, or html: /gradient.png?color1=FFFFFFAA&color2=BBBBBB99&width=10&width=10&rotate=90.
* [ImageHandlerSyntax plugin](http://www.imageresizing.net/plugins/imagehandlersyntax) - Migrate websites from other image resizing handlers without breaking any URLs.
* [IEPngFix plugin](http://www.imageresizing.net/plugins/iepngfix)(New in V3.1.3) - Automatically serve GIF versions of PNG files to IE6 and below clients. Opt-in or opt-out, very configurable.
* [Image404 plugin](http://www.imageresizing.net/plugins/image404) - Supply default images instead of a 404 when an image is missing.
* [Logging plugin](http://www.imageresizing.net/plugins/logging)(v3.1+) - Allows logging through NLog.
* [PdfRenderer](http://www.imageresizing.net/plugins/pdfrenderer) (New in V3.1.3)- Render PDFs to images dynamically, then crop or process them as an image.
* [Presets plugin](http://www.imageresizing.net/plugins/presets) (v3.1+)- Created named settings groups and and reference them with ?preset=name instead of specifying them all in the URL.
* [SizeLimiting plugin](http://www.imageresizing.net/plugins/sizelimiting) - (default) - Limit maximum resolution of photos, or the total size of all processed images.
* [SpeedOrQuality](http://www.imageresizing.net/plugins/speedorquality) (v3.1+) - Gain a 15-30% speed boost by sacrificing rendering quality.
* [VirtualFolder plugin](http://www.imageresizing.net/plugins/virtualfolder) - Create an IIS-like virtual folder that works in Cassini as well as IIS, and doesn't require IIS configuration.

#### Deprecated Plugins

* [AzureReader plugin](http://www.imageresizing.net/plugins/azurereader) - Allows blobstore images to be resized and served. (Azure 1.X compatible) *Deprecated in favore of AzureReader2.* Available at the Performance level.
* [S3Reader plugin](http://www.imageresizing.net/plugins/s3reader) - Process and resize images located on a remote Amazon S3 bucket. A VirtualPathProvider. Works best when combined with DiskCache. *Deprecated in favor of S3Reader2.* Available at the Performance level.
* [PsdReader plugin](http://www.imageresizing.net/plugins/psdreader) - Adds support for PSD source files. *Deprecated in favor of FreeImageDecoder and PsdComposer.* Available at the Elite level.
* [AutoRotate plugin](http://www.imageresizing.net/plugins/autorotate) - Use the EXIF rotation data from the camera to auto-rotate your images. Now included in Core, not a plugin.


<a name="full_command"></a>

## Full Command Reference

#### Selecting a frame or page

* **frame=x** – Choose which frame of an animated GIF to display.
* **page=x**– Choose which page of a multi-page TIFF document to display.

#### Rotation & flipping

* **autorotate=true** Automatically rotates the image based on the EXIF info from the camera. **autorotate.default=true** will only autorotate if the image is processed.
* **sflip=none\|x\|y\|xy** Flips the source image prior to processing (new in V3.1).
* **srotate=0\|90\|180\|270** Rotates the source image prior to processing (only 90 degree intervals) (new in V3.1).
* **rotate=degrees** – Rotates the image any arbitrary angle (occurs after cropping).
* **flip=none\|x\|y\|xy** - Flips the image after everything is done.

#### Manual cropping

* **crop=(x1,y1,x2,y2)** – Crop the image to the specified rectangle on the source image. You can use negative coordinates to specify bottom-right relative locations.
* **cropxunits** The width which the x1 and x2 coordinates are relative to, e.g., use '100' to make x1 and x2 percentages. Useful when you don't know the original image size.
* **cropyunits** The height which the y1 and y2 coordinates are relative to, e.g., use '100' to make y1 and y2 percentages. Useful when you don't know the original image size.


#### Sizing (and padding, autocropping, carving and stretching)

*Please note that width/height/maxwidth/maxheight do NOT include border, margin, or padding widths, and do not include the extra space used by rotation.* They constrain the image, not the canvas.

* **maxwidth, maxheight** – Fit the image within the specified bounds, preserving aspect ratio.
* **width, height** – Force the final width and/or height to certain dimensions. Whitespace will be added if the aspect ratio is different.
* **mode=max\|pad\|crop\|carve\|stretch** - How to handle aspect-ratio conflicts between the image and width+height. 'pad' adds whitespace, 'crop' crops minimally, 'carve' uses seam carving, 'stretch' loses aspect-ratio, stretching the image. 'max' behaves like maxwidth/maxheight (new in V3.1).
* **anchor=topleft\|topcenter\|topright\|middleleft\|middlecenter\|middleright\|bottomleft\|bottomcenter\|bottomright** How to anchor the image when padding or cropping (new in V3.1).
* **scale=both\|upscaleonly\|downscaleonly\|upscalecanvas** – By default, images are never upscaled. Use &scale=both to upscale images if they are smaller than width and height.
* **zoom=0..infinity** - Scale the image by a multiplier. Useful for mobile devices and situations where you need to retain all the existing width/height/crop settings, but scale the result up or down. Defaults to 1. 0.5 produces a half-size image, 2 produces a double-size image.

#### Border, padding, margins and background colors

* **bgcolor=color name \| hex code (6-char).** Sets the background/whitespace color.
* **paddingWidth=px & paddingColor=color\|hex** paddingColor defaults to bgcolor, which defaults to white.
* **borderWidth=px, borderColor=color\|hex** borderColor defaults to transparent.
* **margin=3 or margin=5,5,10,10** Specify a universal margin or left,top,right,bottom widths (new in V3.1.

#### Output format

* **format=jpg\|png\|gif** - The output format to use.
* **quality** - Jpeg compression: 0-100 100=best, 90=very good balance, 0=ugly.
* **colors=2-255** – Control the palette size of PNG and GIF images. If omitted, PNGs will be 24-bit ([PrettyGifs plugin](http://www.imageresizing.net/plugins/prettygifs) required).

#### Misc

* **ignoreicc** - true|false. If true, the ICC profile embedded in the source image will be ignored.
* **cache** - always|no|default - Always forces the image to be cached even if it wasn't modified by the resizing module. Doesn't disable caching if it was modified.
* **process** - always|no|default - Always forces the image to be re-encoded even if it wasn't modified. Does not prevent the image from being modified.
* **dpi=90\|300\|600** - The DPI at which the image should be printed. Ignored by all browsers, most operating systems, and most printers.

#### [Watermark plugin](http://www.imageresizing.net/plugins/watermark)

* **watermark** - The name of one or more watermark layers (or layer groups) to render.

#### [Image404 plugin](http://www.imageresizing.net/plugins/image404)

* **404** - The path to the fallback image, or a named preset.

#### [Gradient plugin](http://www.imageresizing.net/plugins/gradient)

* **color1,color2** - 6 or 8 digit hex values specifying the start and end gradient colors. 
* **angle** - 0 is horizontal. Degrees to rotate the gradient.
* **width/height** - The size of the gradient image.

#### [PrettyGifs plugin](http://www.imageresizing.net/plugins/prettygifs)

* **colors** - 2 to 256 (the number of colors to allow in the output image palette). For Gifs and 8-bit PNGs.
* **dither**=true|false|4pass|30|50|79|[percentage] - How much to dither.

#### [SimpleFilters plugin](http://www.imageresizing.net/plugins/simplefilters)

* &s.grayscale=true|y|ry|ntsc|bt709|flat  (true, ntsc, and y produce identical results)
* &s.sepia=true
* &s.alpha= 0..1
* &s.brightness=-1..1
* &s.contrast=-1..1
* &s.saturation=-1..1
* &s.invert=true

#### [AdvancedFilters plugin](http://www.imageresizing.net/plugins/advancedfilters)

* &a.blur=radius - Gaussian blur with adjustable radius.
* &a.sharpen=radius - Gaussian sharpen with adjustable radius.
* &a.contrast=-1..1
* &a.saturation=-1..1
* &a.brightness=-1..1
* &a.equalize=true - Adjusts contrast, saturation, and brightness with curves based on the histogram. Good for adjusting slightly foggy or dark daytime photos. 
* &a.sepia=true - Sepia effect, slightly different from the one in SimpleFilters.
* &a.oilpainting=1..100 -Try `1` for impressionist, `100` for modern art :)
* &a.removenoise=1-100 - Not a blur effect - designed to remove color noise, 'pepper noise'. Very conservative, doesn't affect edges.

#### [DropShadow plugin](http://www.imageresizing.net/plugins/dropshadow)

* **shadowWidth** - Width of the shadow.
* **shadowOffset** - (x,y) how to offset the drop shadow.
* **shadowColor** - Named or hex color of the shadow.

#### [SpeedOrQuality plugin](http://www.imageresizing.net/plugins/speedorquality)

* **speed**=0..5 - The amount of quality to sacrifice for speed - each value uses different settings and techniques, and may not support all features.

#### [Presets plugin](http://www.imageresizing.net/plugins/presets)

* **preset**=name1,name2,name3 - A list of preset settings groups to apply. 

#### [WhitespaceTrimmer plugin](http://www.imageresizing.net/plugins/whitespacetrimmer)

* **trim.threshold=80** - The threshold to use for trimming whitespace.
* **trim.percentpadding=0.5** - The percentage of padding to restore after trimming.

#### [WicBuilder](http://www.imageresizing.net/plugins/wic) & [FreeImageBuilder](http://www.imageresizing.net/plugins/freeimage)

* **builder=freeimage\|wic** - Enables the FreeImage or Wic pipeline instead of the default GDI pipeline. Special effect plugins not supported.

#### [FreeImageDecoder](http://www.imageresizing.net/plugins/freeimage), [WicDecoder](http://www.imageresizing.net/plugins/wic)

These act as fallback decoders, but you can tell them to try first by using 

* **decoder=wic\|freeimage**

#### [FreeImageEncoder](http://www.imageresizing.net/plugins/freeimage), [WicEncoder](http://www.imageresizing.net/plugins/wic)

In addition to jpeg quality and gif/png colors, you can configure the jpeg subsampling for both Wic and FreeImage.

* **subsampling**==444|422|420

<a name="managed_api"></a>

## Managed API examples

Most tasks with the managed API only require one line:
```c#
  ImageResizer.ImageBuilder.Current.Build(object source, object dest, ResizeSettings settings)
  
  or
  
  Bitmap b = ImageResizer.ImageBuilder.Current.Build(object source, ResizeSettings settings)
  
```

#### Object Source

May be a physical path (C:\..), an app-relative virtual path (~/folder/image.jpg), an Image, Bitmap, Stream, VirtualFile, or HttpPostedFile instance. 

#### Object Dest

May be a Stream instance, a physical path, or an app-relative virtual path.

#### ResizeSetting Settings

ResizeSettings is a friendly wrapper for a query string which provides named properties as well as the regular NameValueCollection interface.

You can create one like so:
```c#

  new ResizeSettings("maxwidth=200&maxheight=200")
  
  //or
  new ResizeSettings(Request.QueryString)
  
  //or
  var r = new ResizeSettings();
  r.MaxWidth = 200;
  r.MaxHeight = 300;
  
```

#### Examples

```c#
  using ImageResizer;
  
  //Converts a jpeg into a png
  
  ImageBuilder.Current.Build("~/images/photo.jpg","~/images/photo.png", 
                             new ResizeSettings("format=png"));
  
  //Crops to a square (in place)
  ImageBuilder.Current.Build("~/images/photo.jpg","~/images/photo.jpg", 
                             new ResizeSettings("width=100&height=200&crop=auto"));
``` 

#### Using Variables in the Destination Path (3.1.3+)

Variables include the correct extension <ext>, random GUID <guid>, source path <path>, source filename <filename>, <width>, <height>, and any settings value <settings.*>. 

This makes many scenarios much easier to code, and reduces room for error. Many users make critical errors in their upload code, such as not sanitizing filenames, or using the original extension (immediate server highjacking, here we go). 

With the new feature, a proper upload system is 3 lines:
```c#
    ImageJob i = new ImageJob(file, 
    "~/uploads/<guid>.<ext>", 
    new ResizeSettings("width=1600")); 
    i.CreateParentDirectory = true;
    i.Build();
```

You can also filter values. `<filename:A-Za-z0-9>` keeps only the alphanumeric characters from the original filename.

<a name="config_reference"></a>

## Configuration Reference

The following is a basic, typical configuration of v4.

```xml
  <?xml version="1.0" encoding="utf-8" ?>
  <configuration>
    <configSections>
      <section name="resizer" type="ImageResizer.ResizerSection,ImageResizer"  requirePermission="false" />
    </configSections>

    <resizer>
      <!-- Unless you (a) use Integrated mode, or (b) map all requests to ASP.NET, 
           you'll need to add .ashx to your image URLs: image.jpg.ashx?width=200&height=20 
           Optional - this is the default setting -->
      <pipeline fakeExtensions=".ashx" defaultCommands="autorotate.default=true" />

      <plugins>
        <add name="DiskCache" />
        <add name="PrettyGifs" />
      </plugins>  
    </resizer>

    <system.web>
      <httpModules>
        <!-- This is for IIS7/8 Classic and Cassini-->
        <add name="ImageResizingModule" type="ImageResizer.InterceptModule"/>
      </httpModules>
    </system.web>

    <system.webServer>
      <validation validateIntegratedModeConfiguration="false"/>
      <modules>
        <!-- This is for IIS7/8 Integrated mode -->
        <add name="ImageResizingModule" type="ImageResizer.InterceptModule"/>
      </modules>
    </system.webServer>
  </configuration>
```
<a name="tricks"></a>

# Nifty Tricks

<a name="watermark"></a>

## Watermark Images Based on Folder Name or Display Size

The *PostRewrite* event is the last of the URL rewriting events, and can be used to enforce certain settings.

You can add an event handler during the Application\Start method in Global.asax.cs.

The following sample code applies a watermark to all images inside 'folder' that are probably above 100x100. I say probably, because the size estimation is based on the assumption that the original image is a 600x600 square. Given a 600x100 actual image size and the querystring "?height=99", the image could escape watermarking and display at 594x99.

So, with this code, you can only know that *one* of the dimensions will be less than 100px - you can't know that both will be.
```c#
  Config.Current.Pipeline.PostRewrite += delegate(IHttpModule sender, HttpContext context, IUrlEventArgs ev) {
      //Check folder
      string folder = VirtualPathUtility.ToAbsolute("~/folder");
      if (ev.VirtualPath.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) {
          //Estimate final image size, based on the original image being 600x600. 
          Size estimatedSize = ImageBuilder.Current.GetFinalSize(new System.Drawing.Size(600,600),
              new ResizeSettings(ev.QueryString));
          if (estimatedSize.Width > 100 || estimatedSize.Height > 100){
              //It's over 100px, apply watermark
              ev.QueryString["watermark"] = "Sun_256.png";
          }
      }
  };
```

#### Important note

While the above enforces watermarking on all processed images, the `process=no` command can disable processing of the image completely, avoiding all resizing and watermarking.

To prevent this, you should add some more code inside PostRewrite
```c#
  Config.Current.Pipeline.PostRewrite += delegate(IHttpModule sender, HttpContext context, IUrlEventArgs ev) {
      //Check folder
      string folder = VirtualPathUtility.ToAbsolute("~/folder");
      if (ev.VirtualPath.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) {
          //Estimate final image size, based on the original image being 600x600.
          Size estimatedSize = ImageBuilder.Current.GetFinalSize(new System.Drawing.Size(600,600),
                          new ResizeSettings(ev.QueryString));
          if (estimatedSize.Width > 100 || estimatedSize.Height > 100){
              //It's over 100px, apply watermark
              ev.QueryString["watermark"] = "Sun_256.png";
              //Force processing if it's an image
              if (Config.Current.Pipeline.IsAcceptedImageType(ev.VirtualPath))
                ev.QueryString["process"] = "Always";
          }
      }
  };
```

<a name="thumbnails"></a>

## Generate Thumbnails and Multiple Sizes to Disk

While the ImageResizer shines at on-the-fly image processing, you can also use it to drastically simplify pre-processing and ahead-of-time resizing as well. **We strongly recommend using the dynamic method instead of pre-generating your images, as pre-generating versions reduces agility and flexibility.** In the examples below, only one line is required to perform all the image decoding, format conversion, processing, resizing, and re-encoding. The rest is path/filename logic. Two different solutions are presented - 1 for processing images as they are uploaded, and another for processing images that are already saved to disk.

#### During Upload

This method generates 3 versions of an image as it is uploaded, adding a \_thumb, \_medium, and \_large suffix to each filename. Uploaded files are named using a generated GUID, as uploaded file names are never safe for use as-is. Even with proper sanitization (alphanumeric filtering AND length limiting), you will encounter duplicates using uploaded filenames on your server.
```c#
  Dictionary<string, string> versions = new Dictionary<string, string>();
  //Define the versions to generate
  versions.Add("_thumb", "width=100&height=100&crop=auto&format=jpg"); //Crop to square thumbnail
  versions.Add("_medium", "maxwidth=400&maxheight=400&format=jpg"); //Fit inside 400x400 area, jpeg
  versions.Add("_large", "maxwidth=1900&maxheight=1900&format=jpg"); //Fit inside 1900x1200 area
  
  //Loop through each uploaded file
  foreach (string fileKey in HttpContext.Current.Request.Files.Keys) {
      HttpPostedFile file = HttpContext.Current.Request.Files[fileKey];
      if (file.ContentLength <= 0) continue; //Skip unused file controls.
      
      //Get the physical path for the uploads folder and make sure it exists
      string uploadFolder = MapPath("~/uploads");
      if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
      
      //Generate each version
      foreach (string suffix in versions.Keys) {
          //Generate a filename (GUIDs are best).
          string fileName = Path.Combine(uploadFolder, System.Guid.NewGuid().ToString() + suffix);

          //Let the image builder add the correct extension based on the output file type
          fileName = ImageBuilder.Current.Build(file, fileName, new ResizeSettings(versions[suffix]), false, true);
      }
      
  }

```

#### After Upload

This example method generates 3 versions of the specified file, and returns a list of the final path names.

For example,
```c#
  GenerateVersions("~/images/image.jpg")
```
Will generate

```
/images/image\_thumb.jpg
/images/image\_medium.jpg
/images/image\_large.jpg
```

And will return a list of those paths.

```c#
  public IList<string> GenerateVersions(string original) {
      Dictionary<string, string> versions = new Dictionary<string, string>();
      //Define the versions to generate and their filename suffixes.
      versions.Add("_thumb", "width=100&height=100&crop=auto&format=jpg"); //Crop to square 
      versions.Add("_medium", "maxwidth=400&maxheight=400format=jpg"); //Fit inside 400x400
      versions.Add("_large", "maxwidth=1900&maxheight=1900&format=jpg"); //Fit inside 1900x1200


      string basePath = ImageResizer.Util.PathUtils.RemoveExtension(original);

      //To store the list of generated paths
      List<string> generatedFiles = new List<string>();

      //Generate each version
      foreach (string suffix in versions.Keys)
          //Let the image builder add the correct extension based on the output file type
          generatedFiles.Add(ImageBuilder.Current.Build(original, basePath + suffix, 
            new ResizeSettings(versions[suffix]), false, true));

      return generatedFiles;   
  }
```
<a name="resize_upload"></a>

## Convert and Resize Images as They Are Uploaded

Resizing and processing images as they are uploaded is very straightforward. Most of the required code is about paths and directories.

The following sample code generates a GUID filename for each upload, determines the appropriate file extension that is needed, then resizes/crops/formats the image according to the specified ResizeSettings.
```c#
  //Loop through each uploaded file
  foreach (string fileKey in HttpContext.Current.Request.Files.Keys) 
  {
    HttpPostedFile file = HttpContext.Current.Request.Files[fileKey];
    if (file.ContentLength <= 0) continue; //Skip unused file controls.
    
    //The resizing settings can specify any of 30 commands.. See http://imageresizing.net for details.
    //Destination paths can have variables like <guid> and <ext>, or 
    //even a santizied version of the original filename, like <filename:A-Za-z0-9>
    ImageResizer.ImageJob i = new ImageResizer.ImageJob(file, "~/uploads/<guid>.<ext>", new ImageResizer.ResizeSettings( 
                "width=2000;height=2000;format=jpg;mode=max"));
    i.CreateParentDirectory = true; //Auto-create the uploads directory.
    i.Build();
  }
```

#### For VB.NET Users

```vbnet
  'Loop through each uploaded file
  For Each fileKey As String In HttpContext.Current.Request.Files.Keys
      Dim file As HttpPostedFile = HttpContext.Current.Request.Files(fileKey)
      If (file.ContentLength > 0) Then 'Skip unused file controls.
        'The resizing settings can specify any of 30 commands.. See http://imageresizing.net for details.
        'Destination paths can have variables like <guid> and <ext>, or 
        'even a santizied version of the original filename, like <filename:A-Za-z0-9>
        Dim i As ImageResizer.ImageJob = New ImageResizer.ImageJob(file, "~/uploads/<guid>.<ext>", New ImageResizer.ResizeSettings("width=2000;height=2000;format=jpg;mode=max"))
        i.CreateParentDirectory = True 'Auto-create the uploads directory.
        i.Build()
      End If
  Next
```
<a name="troubleshooting_guide"></a>

# Troubleshooting

<a name="error_messages"></a>

## Getting Detailed Error Messages

If some images are not displaying correctly, you *must* visit the image url directly to get the exact error message.

The image URL is **not** the same as the page URL; viewing the broken image icons on the parent page doesn't tell us anything useful.

#### How to Open the Image URL Directly

* In Chrome, right-click on the broken image and choose "Open Image in New Tab".
* In Firefox, right-click and choose "View Image".
* In IE, right-click the image and choose "Properties". Copy and paste the "Address" (URL) field contents into the address bar of a new window.

#### Once You're Viewing the Image URL Directly

If you do not get a specific error message, you must enable detailed error messages on your ASP.NET site.

If you have local access to the server, you can set the [customErrors mode](http://msdn.microsoft.com/en-us/library/h0hfz6fc%28v=vs.100%29.aspx) to RemoteOnly and access the URLs using "localhost".

Otherwise, you may need to temporarily set customErrors to "Off", so you can get error messages from a remote location. **Temporarily is the key word!** Detailed error messages are considered a security risk and have enabled certain types of attacks to function. They should not be enabled for more than a few hours at most on a publicly accessible server.

You may also have to **temporarily** change &lt;deployment retail to "False" for the customErrors setting to take effect. 

The customErrors setting is case-sensitive; use "Off", "On", and "RemoteOnly".


<a name="self_diagnostics"></a>

## Accessing Self-Diagnostics

Most configuration and plugin installation issues can be resolved by checking ImageResizer’s self-diagnostics page. If your local website is running at `http://localhost:5000/`, then you can visit it at `http://localhost:5000/resizer.debug.ashx`.

If you’re not using ImageResizer from a web app, you can access the page as a string via `ImageResizer.Configuration.Config.Current.GetDiagnosticsPage()` or write it to disk with `ImageResizer.Configuration.Config.Current.WriteDiagnosticsTo(string path)`. 

#### Diagnostics Page Not Working?

By default, the Diagnostics plugin uses the same setting as [customErrors](http://msdn.microsoft.com/en-us/library/h0hfz6fc%28v=vs.100%29.aspx) (which defaults to Localhost). Thus, if you can see ASP.NET error messages, you will also be able to get the diagnostics page. This ensures that the diagnostics page never exposes data to a host that doesn't already have access to detailed error messages. 

To override, add one of the following to the &lt;resizer&gt; section.
```xml
  <diagnostics enableFor="AllHosts" />
  <diagnostics enableFor="Localhost" />
  <diagnostics enableFor="None" />

```

<a name="troubleshooting_list"></a>

## What Might Be Wrong

To get support or use this guide, make sure you get the detailed error message from visiting the image URL directly. This guide cannot offer a solution to a generic 500 error, 404 error, or a "broken image icon", as those symptoms are far too generic to be useful. The troubleshooting tips presented here are specific to v4. Switch to the v3 troubleshooting page if you are using v3.

If this page doesn't resolve your issue, [visit the Support page](http://www.imageresizing.net/support) for information about the bug bounty and free support requirements.

#### Server Error in '/' Application. Out of memory.

You might be trying to resize certain large images for the first time. Even a 15MB jpg, however, uncompresses to about 80MB in bitmap form (depending on the compression level). If you are resizing to a 2MB jpg (15MB BMP), memory requirements for the operation are roughly 110MB (15 + 80 + 15). If you plan on using ImageResizer for very high-resolution photos (above 8MP), we suggest making sure you have ample amounts of RAM. 400MB to 1GB is usually plenty for the average web site with disk caching enabled.

#### Could not load type 'ImageResizer.InterceptModule'

Potential causes:

1. Your website has  a 'sub-site' (Application Folder) inside it. Application Folders inherit all Web.config settings from their parent sites, yet expect to have their own copies of all the dlls referenced by those settings in their own /bin folder. You can resolve this problem by (a) changing the app folder to a virtual folder, (b) adding a copy of ImageResizer.dll and plugins into the /bin folder inside that application also, or (c) using `<remove />` statements in the child Web.config to cancel out the inherited `<add />` statements from the parent Web.config. Option (c) will disable image resizing within the sub-application.
2. You didn't copy ImageResizer.dll into the /bin folder.
3. The ImageResizer.dll file in /bin is corrupt. Verify it has the correct file size and version number, or re-copy it from the download. 

#### The type or namespace name "ImageResizer" could not be found

When using the ImageResizer from a .NET project that is not a web project you may get the following build error:

> The type or namespace name "ImageResizer" could not be found (are you missing a using directive or assembly reference?)

This is caused by using a Client Profile version of .NET instead of the Full version. You can change this in Project Properties -> Application -> Target Framework. The ImageResizer requires the full version, as it is also designed to support ASP.NET usage and references the System.Web assembly (which is not part of the client profile version of .NET).

#### Image appears original size

Potential causes: 

1. You did not register the HttpModule properly in both places of your Web.config file. Verify you can access /resizer.debug. If not, this is the problem.
2. You are the IIS Classic Pipeline, and are not using the .jpg.ashx syntax, and you have not [mapped all requests to the ASP.NET runtime](http://www.imageresizing.net/docs/cleanurls). 
4. You are mistyping the querystring commands. 
5. The original image is smaller than the size you are requesting, and you are not using `&scale=both` (The default behavior is to never upscale images, [but this can be changed](http://www.imageresizing.net/plugins/defaultsettings)).

#### File not found

Potential causes:

1. You did not register the HttpModule properly in both places of your Web.config file.  Verify you can access /resizer.debug. If not, this is the problem.
2. You (a) aren't specifying a command string, (b) have Precompilation enabled, and (c) are using an image provider. [This is caused by a long-standing bug in the .NET framework](http://stackoverflow.com/questions/12397/net-virtualpathproviders-and-pre-compilation).
3. You have a URL rewriting event that is affecting path names.

#### This type of page is not served. (HTTP 403 error)

  Description: The type of page you have requested is not served because it has been explicitly forbidden.  The extension '.jpg' may be incorrect.   Please review the URL below and make sure that it is spelled correctly. 

Possible causes

1. You aren't logged in. ImageResizer obeys your URL Authorization rules, so don't expect to view images where you can't visit .aspx pages. 
2. You are trying to access a S3 bucket or Remote URL that is not authorized.

This generic error message often hides a more descriptive message, but that message is always guaranteed to be a "Not Authorized To View this Content" kind of error.


#### Server object error 'ASP 0178 : 80070005'

  Server object error 'ASP 0178 : 80070005'
  Server.CreateObject Access Error
  The call to Server.CreateObject failed while checking permissions. 
  Access is denied to this object.

This error usually means that the user that the ASP website is running under does not have NTFS permissions to the ImageResizer dlls. 
Right click the `C:\Program Files\ImageResizingNet\v3` folder and choose Properties, Security, hit Edit, then click Add, type in the user name your website is running under, hit OK, then check Read & Execute, and hit OK, then Apply.

On IIS6, this [account is typically IUSR_ComputerName](http://www.microsoft.com/technet/prodtechnol/WindowsServer2003/Library/IIS/3648346f-e4f5-474b-86c7-5a86e85fa1ff.mspx?mfr=true), but on IIS7, the account is usually `NETWORK SERVICE` or (if you're not use a default app pool), a custom user account. You'll need to open IIS and inspect the appropriate Application Pool to find out which account you need to give permissions to. 

If that fails, providing Readonly access to the Everyone group should work, but that may not be acceptable if you have highly-isolated application pools which you don't want to be able to access the ImageResizer dlls files.

If you still encounter issues, perform a reinstall with COMInstaller.exe, and save the install log. If the reinstall doesn't fix the problem, send the install log to support@imageresizing.net to get help with your issue.

#### Quality loss when resizing 8-bit Grayscale Jpeg images

This is a known bug in GDI+. GDI+ opens 8-bit grayscale Jpeg images as 4-bit images. Here's the [bug report at Microsoft Connect](http://connect.microsoft.com/VisualStudio/feedback/details/649605/gdi-and-wic-opening-an-8bpp-grayscale-jpeg-results-in-quality-loss).

The workaround is to use WIC or FreeImage for these requests. Any of the following plugins will solve the problem

1. WicDecoder - Install, then add `&decoder=wic` to affected URLs. (best quality)
2. WicBuilder - Install, then add `&builder=wic` to affected URLs. Faster, but slightly lower quality than #1 (nearly imperceptible)
3. FreeImageDecoder - Install, then add `&decoder=freeimage`
4. FreeImageBuilder - Install, then add `&builder=freeimage`. Slowest, but highest quality.
 with `builder=freeimage` or `builder=wic`

#### Performance issues or error messages when using a SAN.

This is tricky to get right - [read the full article here](http://imageresizing.net/docs/howto/avoid-network-limit).

#### Losing transparency when working with GIF images

You must have the [PrettyGifs](http://www.imageresizing.net/plugins/prettygifs) plugin installed to get high-quality, transparent GIF and 8-bit PNG results. You may also want the [AnimatedGifs](http://www.imageresizing.net/plugins/animatedgifs) plugin.

#### SizeLimitException - The dimensions of the output image (4800x2700) exceed the maximum permitted dimensions of 3200x3200.

By default, ImageResizer limits the output size of images to 3200x3200. This can be changed [by configuring (or removing) the SizeLimiting plugin](http://www.imageresizing.net/plugins/sizelimiting).



<a name="everything_else"></a>

# Everything Else

<a name="licensing_and_contract_info"></a>

## Licensing and Support Contract Information

## Licenses

The short version: ImageResizer has several license packages that have different tiers of access to our plugins, including free packages in our [essential](http://imageresizing.net/plugins/editions/free) and [trial](http://imageresizing.net/licenses/trial) packages. To find out about our license packages, [visit our website](http://imageresizing.net/licenses).

We also offer email support plans and custom support contracts. Contact us at sales@imazen.io to learn more. 

<a name="contact"></a>

## Contact Us

We can be reached at support@imazen.io. We usually respond within 2 or 3 business days. Of course, you could also check out StackOverflow, where over [500 ImageResizer questions have been answered](http://stackoverflow.com/questions/tagged/imageresizer).
