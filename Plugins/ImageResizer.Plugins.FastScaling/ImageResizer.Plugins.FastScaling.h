// ImageResizer.Plugins.FastScaling.h

#pragma once

/*
Group: Types

typedef: gdImage

typedef: gdImagePtr

The data structure in which gd stores images. <gdImageCreate>,
<gdImageCreateTrueColor> and the various image file-loading functions
return a pointer to this type, and the other functions expect to
receive a pointer to this type as their first argument.

*gdImagePtr* is a pointer to *gdImage*.

(Previous versions of this library encouraged directly manipulating
the contents ofthe struct but we are attempting to move away from
this practice so the fields are no longer documented here.  If you
need to poke at the internals of this struct, feel free to look at
*gd.h*.)
*/
typedef struct gdImageStruct {

	int sx;
	int sy;
	
	/* Truecolor flag and pixels. New 2.0 fields appear here at the
	end to minimize breakage of existing object code. */
	int trueColor;
	int **tpixels;
	/* Should alpha channel be copied, or applied, each time a
	pixel is drawn? This applies to truecolor images only.
	No attempt is made to alpha-blend in palette images,
	even if semitransparent palette entries exist.
	To do that, build your image as a truecolor image,
	then quantize down to 8 bits. */
	int alphaBlendingFlag;
	/* Should the alpha channel of the image be saved? This affects
	PNG at the moment; other future formats may also
	have that capability. JPEG doesn't. */
	int saveAlphaFlag;
}
gdImage;

typedef gdImage *gdImagePtr;


using namespace System;

namespace ImageResizerPluginsFastScaling {

	public ref class Class1
	{
		// TODO: Add your methods for this class here.
	};
}
