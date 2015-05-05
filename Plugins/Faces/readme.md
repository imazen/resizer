Edition: elite
Tags: plugin
Tagline: Provides face detection 
Aliases: /plugins/faces

# Faces plugin

You can find a sample project for this plugin in `\Samples\ImageStudio` within the full download 

Human face detection plugin. Provides automatic face detection, as well as the CropAround plugin, which can even be combined in a single request (using &c.focus=faces) to provide face-focused/face-preserving cropping.

OpenCV is required for face detection. Requires V3.2 or higher.

A NuGet package for this plugin is not available, due to the vast number of dependencies. 

OpenCV does not support being used from multiple app domains. If you get a "Type Initializer Exception", restart the application pool and verify that it only contains 1 application, and that overlapped recycle is disabled.

You **must** disable overlapped recycling on the application pool running this plugin. OpenCV cannot handle multiple instances per plugin.

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

`f.minsize=0..100` (defaults to 3). The smallest face to detect, as a percentage of the image size.

`f.faces=min-count,maxcount` Defaults to 1,8. The minimum and maximum number of faces to detect in the image. 

`f.expand=percent|xpercent,ypercent` Defaults to 0,0. The percent (0..100) to expand the face rectangles in each orientation. If ypercent is omitted, the value from xpercent will be used.

`f.threshold=value|minvalue,value` The confidence threshold required to consider a face detected. Defaults to 1,2. 'minvalue' is used if we have not reached the quote specified in `f.faces`.


## Installation. 

1. Add ImageResizer.Plugins.Faces.dll to your project using Visual Studio. If you copy & paste to /bin, you'll need to also copy the files listed under Managed Dependencies.
2. Add `<add name="Faces" downloadNativeDependencies="true" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.
3. If you're not comfortable allowing the plugin to automatically download the correct bitness versions of the unmanaged dependencies, then set downloadNativeDependencies="false" and keep reading.
3. Manually copy the required xml files to the /bin folder of your application (see *Feature classification files*)
4. Manually copy all required dlls to the /bin folder of your application. (see *Using the 2.3.1 pre-compiled binaries*)



## Managed Dependencies

* ImageResizer.dll
* AForge.dll
* AForge.Math.dll
* AForge.Imaging.dll
* AForge.Imaging.Formats.dll 
* OpenCvSharp.dll
* OpenCvSharp.dll.config
* Newtonsoft.Json.dll

## JSON member reference (for both Faces and RedEye plugins)

The JSON response contains image layout information so StudioJS or ImageResizer.js can translate between source coordinates (in which all face and red-eye rectangles are stored) and destination coordinates (which are used for input and display).


* ow/oh - original image width/height
* cropx/cropy/cropw/croph - Source rectangle on original image that has been cropped/copied to the result image
* dx/dy/dw/dh - Destination rectangle on result image that contains the imagery from cropx/cropy/cropw/cropg. If rotation is used, this will be the bounding box.
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

For RedEye results, only rectanges where Feature=0 are eyes. Feature=1 means Eye Pair, Feature = 2 means face.




## Managed Dependencies

* ImageResizer.dll
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

## Using the 2.3.1 pre-compiled binaries

All dlls must match in bitness. All dlls are bitness specific. You can't run OpenCV x86 on an x64 app pool or vice versa. 

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
