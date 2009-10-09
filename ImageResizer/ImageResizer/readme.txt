
**

Overview of source files

**

InterceptModule.cs  Handles interception of incoming requests, and ties the DiskCache, CustomFolders, and ImageManager classes together.

ImageManager.cs     Exposes a BuildImage method that performs a series of operations on one images and returns another. Operations can 
    be specified via querystring or Settings classes. Uses Settings classes internally to parse querystring info.

DiskCache.cs        Handles disk caching, cleanup

CustomFolders.cs    Allows custom syntaxes to be implemented easily -  The querystring can be modified based on any request data. 

ResizeSettings.cs    Parses the resizing settings from the querystring, and handles the math that merges the numbers into data.
ImageSettings.cs     Parses color, padding, border, and shadow commands from the querystring. Also includes ImageFilter class, for future addition of image filters.
WatermarkSettings.cs Extensibility point for post-processing images (as well as modifying settings prior to processing).
ImageOutputSettings.cs Handles format parsing and calculation, encapsulates the writing of single-frame Bitmap objects to disk.

Quantizer.cs, OctreeQuantizer.cs: GIF/PNG8 palette creation and indexing.

PolygonMath.cs       Utility methods used in the mathematical calcuations in ResizeSettings and ImageManger.

yrl.cs               URL parsing and building. 


Known issues:
Content-location for the original image is being sent on IIS7, but not IIS5.1. Unable to reproduce issue...  If you can, let me know.
EXIF data is removed - by design. EXIF data bloats the image file, and can sometimes expose confidential info.

**

Changelog, by version

Changes since RC2 (v2.1a)
************************************

Added manual URL authorization using the ASP.NET UrlAuthorizationModule API. AllowURLRewriting no longer circumvents URL authroization.

Added DisableCustomQuantization setting to allow GIFs to be generated on servers where the Marshal class is prohibited.

Added ResizeExtension=".cd" so IIS5 & 6 configuration is optional. Users can append .cd to the end of the image URL instead of adding a wildcard mapping.
Added dithering.


Changes since RC1
************************************

Fixed rare bug where rounding causes Bitmap to be initialized with a dimension of 0, and causes a Parameter exception. 
Ocurred when resizing an image to < 2px in height or width (usually happens with 2x1000 size images, etc).
Added regression test for 500x2 image resized to 100px wide.

Fixed typo (missing else) in SaveToNonSeekableStream. This method is for extensibility, and is not used by the Resizer directly.
This method is now tested and part of the Regression tests (HandlerTest.ashx).


TODO: Demo other usage scenarios 

Writing to disk from disk
Writing to disk from upload field
Visual upload & crop
Custom handler for off-site images, etc.


**************************** By version changelog
***
Changes since 2.0b
**************************************
Transparency is preserved with GIF files.

Added WatermarkSettings.cs class for watermarking.

Converted ImageManager from a Static class to a normal class with a getBestInstance() static method. Allows the GIF animation plugin to
be added just by copying in the Animation folder.
InterceptManager now uses the getBestInstance() method of ImageManager to call the BuildImage method.


Added support for ?frame=1-x and ?page=1-x. You can now select frames from GIF images and pages from TIFF files. Removed ?time

.tif is now a supported input extension... previously only .tiff and .tff were allowed.

Hashes are now SHA-256 instead of .NET 32-bit. They are base-16 encoded. Base64 needs a case-sensitive filesystem.

Added &dither=true|4pass|percent 
Quantizer and OctreeQuantizer now support adjustable 2-pass and 4-pass dithering, Floyd-Stienburg error diffusion matrix.


****
Changes since 2.0a-mar-4

Fixed rounding error on resizing (caused a black line when when pixels were between .45 and .55)

Added DisableCacheCleanup command, and made MaxCachedImages < 0 behave the same as DisableCacheCleanup=true

Fixed aspect ratio issue if both maxwidth, width, and height are specified.

Added &time to commands, but didn't implement.

Removed uneeded rounding code from ImageManager

****
Changes before 2.0a-mar-4


Fixed UNC paths

2-2-09

FIXED: Cleanup routine can cause bottleneck on GetFiles() - fix so that Directory.GetFiles() only happens at startup and when items are added.


FIXED: Soft border around image. 
Determined that this is not a new bug - existed in 1.2 also. Tried an assortment of experiments, but DrawImage seems determined to alias against non-existent data.
Minimizing .Clear() calls seems to make it hardly noticeable, as before.

FIXED: imagecache/ is not protected when AllowURLRewriting is enabled
http://localhost:57818/resize(40,40)/imagecache/1639776677.jpg
bypasses it.  Added protection in the HttpModule


2-11-09
-- Fixed potention issue in Quantizer.cs that may cause lines
-- Fixed maxwidth/maxheight not getting picked up

-- custom crop coordinates at 0 were being applied in the negative coordinate zone
-- Fixed so x1,y1 weren't affected, but setting x2 and y2 to 0 is bottom-right relative.
-- changed flip, added sourceFlip

2-12-09
-- Added -ignoreicc parameter and made ICC reading the default (but not output)

**

******************* By day log

5-21-09

Removed .pfx and code signing settings.
Fixed upgrade notes link

5-20-09

Transparency is preserved with GIF files.

Added WatermarkSettings.cs class for watermarking.

Converted ImageManager from a Static class to a normal class with a getBestInstance() static method. Allows the GIF animation plugin to
be added just by copying in the Animation folder.

Added support for ?frame=1-x and ?page=1-x. You can now select frames from GIF images and pages from TIFF files. 

.tif is now a supported input extension... previously only .tiff and .tff were allowed.

InterceptManager now uses the getBestInstance() method of ImageManager to call the BuildImage method.

Hashes are now SHA-256 instead of .NET 32-bit. They are base-16 encoded.



5-19-09
Improved fallback color selection during dithering
Switched to base32 for path hash encoding (filesystem is case-insensitive).

5-18-09
Added dithering, animated GIF support, and switched to SHA256 path hashes.

5-15-09
Fixed aspect ratio issue if both maxwidth, width, and height are specified.

Problems with
http://localhost:734/content/00011221_ImageResizervsPhotoshop/quality-original.jpg?width=600&thumbnail=gif&maxwidth=229
but this works
http://localhost:734/content/00011221_ImageResizervsPhotoshop/quality-original.jpg?width=600&format=gif&maxwidth=229

Only happens in IIS7 classic mode...! Seems to use old code from months ago... some assembly must be cached. Very strange.


4-23-09
Added DisableCacheCleanup command, and made MaxCachedImages < 0 behave the same as DisableCacheCleanup=true

Fixed rounding error that could cause a pixel line on the right and/or bottom sides of the image. Rare floating point rounding error.

This fix will be going into the 2.0 beta, but you can apply it yourself.

Line 237 of ImageManger.cs

Replace
Bitmap b = new Bitmap((int)Math.Round(box.Width), (int)Math.Round(box.Height), PixelFormat.Format32bppArgb);
with
Bitmap b = new Bitmap((int)Math.Floor(box.Width), (int)Math.Floor(box.Height), PixelFormat.Format32bppArgb);
instead.

I looked for other places the bug could affect, but I think this is it. System.Drawing is truncating floating point values before rounding, so values very close to .5 can be rounded incorrectly.
This image was resizing to 150x42.527472527472527472527472527473. System.Drawing was rounding down, and the image size was rounding up.


~
Added YRL fix.


2-2-09

FIXED: Cleanup routine can cause bottleneck on GetFiles() - fix so that Directory.GetFiles() only happens at startup and when items are added.


FIXED: Soft border around image. 
Determined that this is not a new bug - existed in 1.2 also. Tried an assortment of experiments, but DrawImage seems determined to alias against non-existent data.
Minimizing .Clear() calls seems to make it hardly noticeable, as before.

FIXED: imagecache/ is not protected when AllowURLRewriting is enabled
http://localhost:57818/resize(40,40)/imagecache/1639776677.jpg
bypasses it.  Added protection in the HttpModule


2-11-09
-- Fixed potention issue in Quantizer.cs that may cause lines
-- Fixed maxwidth/maxheight not getting picked up

-- custom crop coordinates at 0 were being applied in the negative coordinate zone
-- Fixed so x1,y1 weren't affected, but setting x2 and y2 to 0 is bottom-right relative.
-- changed flip, added sourceFlip

2-12-09
-- Added -ignoreicc parameter and made ICC reading the default (but not output)