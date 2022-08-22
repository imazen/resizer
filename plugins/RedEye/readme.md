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

1. Add ImageResizer.Plugins.RedEye.dll to your project using Visual Studio. If you copy & paste to /bin, you'll need to also copy the files listed under Managed Dependencies.
2. Add `<add name="RedEye" downloadNativeDependencies="true" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.
3. If you're not comfortable allowing the plugin to automatically download the correct bitness versions of the unmanaged dependencies, then set downloadNativeDependencies="false" and keep reading.
3. Manually copy the required XML files to the /bin folder of your application (see *Feature classification files*)
4. Manually copy all required DLLs to the /bin folder of your application. (see *Using the 2.3.1 pre-compiled binaries*)



## Managed Dependencies

* ImageResizer.dll
* ImageResizer.Plugin.Faces.dll
* AForge.dll
* AForge.Math.dll
* AForge.Imaging.dll
* AForge.Imaging.Formats.dll 
* OpenCvSharp.dll
* OpenCvSharp.dll.config
* Newtonsoft.Json.dll


## Feature classification files

[You can download all the XML files](http://downloads.imageresizing.net/OpenCV-2.3.1-all-cascades.zip) in a single .ZIP file. You only need to copy the following into the /bin folder.

* haarcascade\_frontalface\_default.xml
* haarcascade\_mcs\_lefteye.xml
* haarcascade\_mcs\_righteye.xml
* haarcascade\_mcs\_eyepair_big.xml
* haarcascade\_mcs\_eyepair\_small.xml

## Using the 2.3.1 pre-compiled binaries

All DLLs must match in bitness. All DLLs are bitness specific. You can't run OpenCV x86 on an x64 app pool or vice versa.

* [Download 32-bit DLLs](http://downloads.imageresizing.net/OpenCv-min-2.3.1-x86.zip).
* [Download 64-bit DLLs](http://downloads.imageresizing.net/OpenCv-min-2.3.1-x64.zip).

## Manually getting the binaries

The provided binaries are for OpenCV 2.3.1. If a newer version is released, you can get it yourself. 

1. Download either the [x86](http://code.google.com/p/opencvsharp/downloads/detail?name=OpenCvSharp-2.3.1-x86-20120218.zip&can=2&q=) or [x64](http://code.google.com/p/opencvsharp/downloads/detail?name=OpenCvSharp-2.3.1-x64-20120218.zip&can=2&q=) build of OpenCvSharp.
2. Extract to a folder, and copy OpenCvSharp.dll and OpenCvSharp.dll.config. The x86 and x64 builds are actually identical. 
3. Go to SourceForge, the opencvlibrary project, the Files section, the opencv-win folder \[[Link](http://sourceforge.net/projects/opencvlibrary/files/opencv-win/)\].
4. Select the latest version and download the OpenCV-[Version]-win-superpack.exe file. 
5. Extract it somewhere (you'll want to delete it later, it's over 1GB uncompressed)

### Files to copy from extracted OpenCV-2.3.1-win-superpack package

* tbb.dll (From opencv\build\common\tbb\ia32\vc9 or opencv\build\common\tbb\intel64\vc9
* opencv\_calib3d231.dll (From opencv\build\x64\vc9\bin or opencv\build\x86\vc9\bin)
* opencv\_core231.dll
* opencv\_features2d231.dll
* opencv\_flann231.dll
* opencv\_gpu231.dll
* opencv\_highgui231.dll
* opencv\_imgproc231.dll
* opencv\_legacy231.dll
* opencv\_ml231.dll
* opencv\_objdetect231.dll
* opencv\_ts231.dll
