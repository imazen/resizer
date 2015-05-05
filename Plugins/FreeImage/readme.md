Tags: plugin
Edition: creative
Tagline: "Includes 4 plugins: FreeImageDecoder adds support for RAW & HDR source images - over 20 formats supported. FreeImageEcoder provides 2-4x Faster jpeg encoding. FreeImageResizer adds support for Lanczos3 and CatmullRom scaling. FreeImageBuilder provides an alternate imaging pipeline."
Aliases: /plugins/freeimage


# FreeImage Plugins

`ImageResizer.Plugins.FreeImage.dll` contains several plugins, all based around the abilities offered by the C/C++ FreeImage library. These plugins are alpha-level. 

## Required files

3 dlls must be placed in the /bin folder of the application for the plugins to work. 

* `ImageResizer.Plugins.FreeImage.dll` (dlls\release)
* `FreeImageNET.dll` (dlls\release)
* `FreeImage.dll` ([download x86 here](http://dyn.imageresizing.net/freeimage/3.15.1.0-custom/x86/FreeImage.dll) or [the 64-bit version here](http://dyn.imageresizing.net/freeimage/3.15.1.0-custom/x64/FreeImage.dll))  **NOTE - you must copy the right bitness for your application pool!** In V3.2 and higher, you can use the downloadNativeDependencies="true" attribute on any of the FreeImage plugins instead of manually downloading those images.

## FreeImageDecoder

Introduces support for RAW & HDR image formats, such as CRW/CR2, NEF, RAF, DNG, MOS, KDC, DCR, etc. Also introduced support for XBM, XPM, TARGA, SGI, Sun RAS, PSD, PICT, PNG, PFM, PBM, PGM, PPM, PCX, MNG, Kodak PhotoCD, KOALA, JPEG-2000, JIF, JNG, IFF, ICO, Raw Fax G3, EXR, DDS, and Dr. Halo CUT files.

Install by adding `<add name="FreeImageDecoder" />` to the plugins section of Web.config. And make sure you've added the 3 required dlls.
  
FreeImageDecoder shows a 50%-400% increase in performance over GDI when loading TIFF files. By default, FreeImageDecoder is a fallback, so if you want it to load TIFF files, add &decoder=freeimage to the querystring (v3.1+). It does not support &page= at the moment.
  
## FreeImageEncoder

FreeImageEncoder can encode jpegs 2-3x as fast as GDI can, and offers more encoding options. 

Install by adding `<add name="FreeImageEncoder" />` to the plugins section of Web.config. And make sure you've added the 3 required dlls.
  
Use by adding `&encoder=freeimage` to the URL.

No support for transparency - so only use it on PNG and GIF files that don't need it.

## FreeImageBuilder

Provides an alternate resizing pipeline that never touches GDI. Only supports width/maxwidth/height/maxheight/scale/marginWidth/paddingWidth/fi.scale settings. Only operates on requests specifying `builder=freeimage`

Install by adding `<add name="FreeImageBuilder" />` to the plugins section of Web.config. And make sure you've added the 3 required dlls.

Not always faster, as FreeImage is slower at scaling images than GDI, even if it has faster encoding/decoding.

Defaults to box scaling

## FreeImageResizer

Adds support for FreeImage resizing algorithms, which include CatmullRom, Lanczos3, bspline, box, bicubic, and bilinear filters.

Installed by adding `<add name="FreeImageResizer" />`
  
Activated by adding &fi.scale=bicubic&#124;bilinear&#124;box&#124;bspline&#124;catmullrom&#124;lanczos command


## License

This set of plugins is part of the [Design](/plugins) bundle, and licensed accordingly. The underlying [native FreeImage dlls are licensed under the FreeImage License](http://freeimage.sourceforge.net/freeimage-license.txt).
