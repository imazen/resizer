Possible issues

GIF/PNG quantitization may not work on x64 - 

Content-location for the original image is being sent on IIS7, but not IIS5.1 

EXIF data is removed.

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