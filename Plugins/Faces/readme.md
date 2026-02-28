Edition: elite
Tags: plugin
Tagline: Provides face detection 
Aliases: /plugins/faces

# Faces plugin

*PLEASE NOTE*
* **This plugin is not forwards-compatible. Avoid these URL commands for maximum compatibility with Imageflow and future major ImageResizer releases.**
* **Do not use with untrusted image data. This plugin relies on third-party C and C++ code which we have not audited (OpenCV).**
* **While we provide a baseline version of OpenCV, we suggest that you check for the latest compatible release, at it may include security fixes.**


You can find a sample project for this plugin in `\Samples\ImageStudio` within the full download 

Human face detection plugin. Provides automatic face detection, as well as the CropAround plugin, which can even be combined in a single request (using &c.focus=faces) to provide face-focused/face-preserving cropping.

OpenCV is required for face detection. 
You **must** disable overlapped recycling on the application pool running this plugin. OpenCV cannot handle multiple instances per plugin.

OpenCV does not support being used from multiple app domains. If you get a "Type Initializer Exception", restart the application pool and verify that it only contains 1 application, and that overlapped recycle is disabled.


## URL Usage

* `f.detect=true` - Causes a JSON response to be returned in the following format:

    {"dx":0.0,"dy":0.0,"dw":604.0,"dh":453.0,"ow":604.0,"oh":453.0,"cropx":0.0,"cropy":0.0,"cropw":604.0,"croph":453.0,
    "features":[{"X":344.0,"Y":73.0,"X2":388.0,"Y2":117.0,"Accuracy":87.0},{"X":159.0,"Y":55.0,"X2":206.0,"Y2":102.0,"Accuracy":82.0},{"X":416.0,"Y":52.0,"X2":459.0,"Y2":95.0,"Accuracy":72.0},{"X":96.0,"Y":54.0,"X2":147.0,"Y2":105.0,"Accuracy":44.0},{"X":467.0,"Y":50.0,"X2":508.0,"Y2":91.0,"Accuracy":30.0},{"X":270.0,"Y":59.0,"X2":311.0,"Y2":100.0,"Accuracy":8.0},{"X":368.0,"Y":270.0,"X2":396.0,"Y2":298.0,"Accuracy":6.0},{"X":238.0,"Y":84.0,"X2":264.0,"Y2":110.0,"Accuracy":5.0}],"message":null}

* `f.getlayout=true` - JSON response is returned with layout data, but no face detection is performed.
* `f.show=true` - Draws green rectangles around all the detected faces in the image

## Managed API Usage

The following convenience methods allow you pass a virtual or physical image path (or stream) into them, along with a NameValueCollection of settings. 

Returns a list of face objects for the given image (path, stream, Bitmap, etc).
Note that the face coordinates are relative to the unrotated, unflipped source image.
ImageResizer.js can *keep* these coordinates synced during rotations/flipping if they are stored in the 'f.rects' querystring key before the 'srotate' or 'sflip' commands are applied.

    Config.Current.Plugins.Get<FacesPlugin>().GetFacesFromImage(object image,NameValueCollection settings); //Returns List<Face>

Returns a comma-delimited list of face coordinates (x,y,x2,y2,accuracy) for the given image (path, stream, Bitmap, etc).
Note that the face coordinates are relative to the unrotated, unflipped source image.
ImageResizer.js can *keep* these coordinates synced during rotations/flipping if they are stored in the 'f.rects' querystring key before the 'srotate' or 'sflip' commands are applied.

    Config.Current.Plugins.Get<FacesPlugin>().GetFacesFromImageAsString(object image,NameValueCollection settings); //Returns string


All tuning parameters are identical between the URL and Managed API.

## Tuning

`f.minsize=0..100` (defaults to 4). The smallest face to detect, as a percentage of the image size.

`f.faces=min-count,maxcount` Defaults to 1,10. The minimum and maximum number of faces to detect in the image. 

`f.expand=percent|xpercent,ypercent` Defaults to 0,0. The percent (0..100) to expand the face rectangles in each orientation. If ypercent is omitted, the value from xpercent will be used.

`f.threshold=value|minvalue,value` The confidence threshold required to consider a face detected. Defaults to 3,5. 'minvalue' is used if we have not reached the quote specified in `f.faces`.


## Installation

1. Add the ImageResizer.Plugins.Faces NuGet package to your project. This will install all required dependencies including OpenCvSharp4 and its native OpenCV binaries.
2. Add `<add name="Faces" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.

The OpenCvSharp4 NuGet packages include `.targets` files that copy native OpenCV binaries to the output directory automatically. Haar cascade XML files are embedded as gzipped resources within the plugin assembly.

**Note:** NuGet `.targets`-based native DLL copying works for direct package references, but may not propagate transitively through project references on older .NET Framework projects. If you reference a class library that depends on this plugin, you may need to install the `OpenCvSharp4.runtime.win` package directly in your startup/web project.



## Managed Dependencies

* ImageResizer.dll
* AForge.dll
* AForge.Math.dll
* AForge.Imaging.dll
* AForge.Imaging.Formats.dll
* OpenCvSharp4 (via NuGet)
* OpenCvSharp4.Extensions (via NuGet)
* OpenCvSharp4.runtime.win (via NuGet)
* Newtonsoft.Json.dll
* System.Drawing.Common.dll

## JSON member reference (for both Faces and RedEye plugins)

The JSON response contains image layout information so StudioJS or ImageResizer.js can translate between source coordinates (in which all face and red-eye rectangles are stored) and destination coordinates (which are used for input and display).


* ow/oh - original image width/height
* cropx/cropy/cropw/croph - Source rectangle on original image that has been cropped/copied to the result image
* dx/dy/dw/dh - Destination rectangle on result image that contains the imagery from cropx/cropy/cropw/croph. If rotation is used, this will be the bounding box.
* message - String containing error message() if any
* features - array of rects describing features. Rect = {X,Y,X2,Y2,Accuracy, (Feature)} 

## Rect reference

Each item in the 'features' array contains the following members

* X
* Y
* X2
* Y2
* Accuracy
* Feature (only for RedEye)

For RedEye results, only rectangles where Feature=0 are eyes. Feature=1 means Eye Pair, Feature = 2 means face.

## Version history

### v4.3 (current)

* **BREAKING**: OpenCvSharp 2.4.10 replaced with OpenCvSharp4 4.10.0 (OpenCV 2.x to 4.x)
* All 5 old OpenCvSharp-WithoutDll assembly references replaced with 3 NuGet PackageReferences (OpenCvSharp4, OpenCvSharp4.Extensions, OpenCvSharp4.runtime.win)
* CDN auto-download of OpenCV native binaries removed. Binaries now come from NuGet packages.
* Haar cascade XML files now embedded as gzipped resources instead of downloaded from CDN
* `downloadNativeDependencies="true"` attribute no longer needed (NuGet handles native dependencies)
* Internal API: `DetectedObject` struct replaces `CvAvgComp`. `DetectFeatures()` takes `Mat` instead of `IplImage`.
* Newtonsoft.Json upgraded from 7.0.1 to 13.0.4
* Added System.Drawing.Common 8.0.0
* Retargeted to .NET Framework 4.7.2

### v4.2.8 and prior

Previous versions used OpenCvSharp 2.4.10 (wrapping OpenCV 2.x) and required manual binary management or CDN auto-download.

**Installation (v4.2.8):**

1. Add ImageResizer.Plugins.Faces.dll to your project. Copy files listed under Managed Dependencies to /bin.
2. Add `<add name="Faces" downloadNativeDependencies="true" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.
3. If not using auto-download, manually copy the required XML files and DLLs (see below).

**Managed Dependencies (v4.2.8):** ImageResizer.dll, AForge.dll, AForge.Math.dll, AForge.Imaging.dll, AForge.Imaging.Formats.dll, OpenCvSharp.dll, OpenCvSharp.dll.config, Newtonsoft.Json.dll

**Feature classification files (v4.2.8):**

* https://d3ndcb4i803ljg.cloudfront.net/opencv/2.4.10/cascades/haarcascade_frontalface_alt.xml
* https://d3ndcb4i803ljg.cloudfront.net/opencv/2.4.10/cascades/haarcascade_eye.xml

**Manually getting the binaries (v4.2.8):**

The provided binaries were for OpenCV 2.4.10.

1. Download [OpenCvSharp 2.4.10](https://github.com/shimat/opencvsharp/releases/tag/2.4.10.20170126).
2. Extract to a folder, and copy OpenCvSharp.dll and OpenCvSharp.dll.config.
3. Go to SourceForge, the opencvlibrary project, the Files section, the opencv-win folder \[[Link](http://sourceforge.net/projects/opencvlibrary/files/opencv-win/)\].
4. Download the OpenCV-[Version]-win-superpack.exe file and extract it.

**Files to copy from extracted package (v4.2.8):**

* tbb.dll
* opencv\_calib3d231.dll (or opencv\_calib3d2410.dll depending on version)
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
