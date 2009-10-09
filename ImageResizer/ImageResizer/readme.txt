Possible issues

Seems to be working: GIF/PNG quantitization may not work on x64 - 

Unable to reproduce: Content-location for the original image is being sent on IIS7, but not IIS5.1 

By design: EXIF data is removed.

5-15-09
Fixed aspect ratio issue if both maxwidth, width, and height are specified.

Problems with
http://nathanaeljones.com/content/00011221_ImageResizervsPhotoshop/quality-original.jpg?width=600&thumbnail=gif&maxwidth=229
but this works
http://nathanaeljones.com/content/00011221_ImageResizervsPhotoshop/quality-original.jpg?width=600&format=gif&maxwidth=229


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