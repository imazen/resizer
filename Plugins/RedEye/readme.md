Tags: plugin
Edition: elite
Tagline: Provides sophisticated eye detection and red eye correction
Aliases: /plugins/redeye


# RedEye plugin

*PLEASE NOTE*
* **This plugin is not forwards-compatible. Avoid these URL commands for maximum compatibility with Imageflow and future major ImageResizer releases.**
* **Do not use with untrusted image data. This plugin relies on third-party C and C++ code which we have not audited (OpenCV).**
* **While we provide a baseline version of OpenCV, we suggest that you check for the latest compatible release, at it may include security fixes.**

You can find a sample project for this plugin in `\Samples\ImageStudio` within the full download 

Provides automatic and manual red-eye detection and correction. For automatic face and eye detection, OpenCV is required.

OpenCV does not support being used from multiple app domains. If you get a "Type Initializer Exception", restart the application pool and verify that it only contains 1 application, and that overlapped recycle is disabled.

You **must** disable overlapped recycling on the application pool running this plugin. OpenCV cannot handle multiple instances per plugin.


## Usage

* r.autoeyes=true
* r.eyes=x1,y1,x2,y2,a,x1,y1,x2,y2,a,... (groups of 5 coordinates, the fifth of which is the accuracy value)
* r.filter=2
* r.detecteyes=true - Responds with a JSON result. See [the Faces plugin documentation for details](/plugins/faces).
* r.getlayout=true - Responds with image layout info in JSON. See [the Faces plugin documentation for details](/plugins/faces).

## Installation

1. Add the ImageResizer.Plugins.RedEye NuGet package to your project. This will install all required dependencies including the Faces plugin, OpenCvSharp4, and its native OpenCV binaries.
2. Add `<add name="RedEye" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.

No manual binary copying is needed. The OpenCvSharp4 NuGet packages handle native OpenCV binary deployment automatically. Haar cascade XML files are embedded as gzipped resources within the plugin assembly.



## Managed Dependencies

* ImageResizer.dll
* ImageResizer.Plugins.Faces.dll
* AForge.dll
* AForge.Math.dll
* AForge.Imaging.dll
* AForge.Imaging.Formats.dll
* OpenCvSharp4 (via NuGet)
* OpenCvSharp4.Extensions (via NuGet)
* OpenCvSharp4.runtime.win (via NuGet)
* Newtonsoft.Json.dll
* System.Drawing.Common.dll


## Version history

### v4.3 (current)

* **BREAKING**: OpenCvSharp 2.x replaced with OpenCvSharp4 4.10.0 (OpenCV 2.x to 4.x), same migration as the Faces plugin
* CDN auto-download of OpenCV native binaries removed. Binaries now come from NuGet packages.
* Haar cascade XML files now embedded as gzipped resources instead of downloaded from CDN
* `downloadNativeDependencies="true"` attribute no longer needed (NuGet handles native dependencies)
* Retargeted to .NET Framework 4.7.2

### v4.2.8 and prior

Previous versions used OpenCvSharp wrapping OpenCV 2.3.1 and required manual binary management or CDN auto-download.

**Installation (v4.2.8):**

1. Add ImageResizer.Plugins.RedEye.dll to your project. Copy files listed under Managed Dependencies to /bin.
2. Add `<add name="RedEye" downloadNativeDependencies="true" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.
3. If not using auto-download, manually copy the required XML files and DLLs (see below).

**Managed Dependencies (v4.2.8):** ImageResizer.dll, ImageResizer.Plugins.Faces.dll, AForge.dll, AForge.Math.dll, AForge.Imaging.dll, AForge.Imaging.Formats.dll, OpenCvSharp.dll, OpenCvSharp.dll.config, Newtonsoft.Json.dll

**Feature classification files (v4.2.8):**

[Download all XML files](http://downloads.imageresizing.net/OpenCV-2.3.1-all-cascades.zip) - the following were needed in /bin:

* haarcascade\_frontalface\_default.xml
* haarcascade\_mcs\_lefteye.xml
* haarcascade\_mcs\_righteye.xml
* haarcascade\_mcs\_eyepair_big.xml
* haarcascade\_mcs\_eyepair\_small.xml

**Pre-compiled OpenCV binaries (v4.2.8):**

All DLLs had to match in bitness. You could not run OpenCV x86 on an x64 app pool or vice versa.

* [Download 32-bit DLLs](http://downloads.imageresizing.net/OpenCv-min-2.3.1-x86.zip)
* [Download 64-bit DLLs](http://downloads.imageresizing.net/OpenCv-min-2.3.1-x64.zip)

Required native DLLs included: tbb.dll, opencv\_calib3d231.dll, opencv\_core231.dll, opencv\_features2d231.dll, opencv\_flann231.dll, opencv\_gpu231.dll, opencv\_highgui231.dll, opencv\_imgproc231.dll, opencv\_legacy231.dll, opencv\_ml231.dll, opencv\_objdetect231.dll, opencv\_ts231.dll
