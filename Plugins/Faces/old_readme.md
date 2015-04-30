
# ImageResizer.Plugins.Faces

	<add name="Faces" downloadNativeDependencies="true" />

This plugin can detect frontal faces in images. It can display these with green rectangles, return a JSON result, or be called from managed code via the FaceDetection class.
It also watches for "c.focus=faces" and replaces 'faces' with a comma-delimtied list of points. 

### Commands

* f.show=true - Displays faces in green rectangles
* f.detect=true - Returns the data in JSON form
* c.focus=faces - Only takes effect if CropAround is installed

### Tuning commands

* f.faces=min,max OR f.faces=target //You must specify how many faces you are wanting to enhance precision. You can specify a range in min,max form, or just specify a certain count, like f.faces=1
* f.threshold=min,target OR f.threshold=min //You can adjust the confidence threshold from (1) lowest to (200) extremely high. No need to specify 'target' unless you are also specifying a range with 'f.faces'.
* f.minsize=3.0 //You can require that detected faces be a minimum percentage of the image size. Defaults to 3% (3.0)
* f.expand=0.1,0.4 //The percentage to expand the faces for buffering

# ImageResizer.Plugins.CropAround

	<add name="CropAround" />

### Commands

* c.focus=x,y or x1,y1,x2,y2 or x1,y1,x2,y2,x3,y3, etc...
* c.zoom=true (defaults to false, minimalist cropping)


---------------------------------------------
Face Search


f.show=true will highlight faces. 
f.detect=true will return the data in JSON form.


Tuning parameters

* max faces desired detected
* min faces desired detected
* min face size (percentage)
* min confidence level
* default confidence level threshold


Managed: See FaceDetection class





API

* Locate faces in image, returning rectangles in both source and final coordinates. (Managed & json API req.d)
* Create CropUp style cropping that takes a point or rectangle to center auto-cropping around. Allow adjustable zoom
* Offer integrated API that combines both...



Orig. Spec:


Features:
1. Offer JSON and Managed API for detecting faces in any given image, and returning 1 or more rectangles in both source image and final image coordinates.
2. Offer automatic cropping mode that uses a provided 'focus rectangle' to crop to a given square or rectangle size. Cropping directly to a face rect will often chop off foreheads and necks, so it's better to center the frame around the detected head and only shrink to a provided percentage away from the 'focus rectangle'.
3. Offer automatic cropping mode that integrates #1 and #2 in a single request 

Parameters

1. Optimize based on expected face count to eliminate false positives