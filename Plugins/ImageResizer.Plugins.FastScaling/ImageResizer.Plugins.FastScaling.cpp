// This is the main DLL file.

#include "stdafx.h"
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include "math.h"
#include "ImageResizer.Plugins.FastScaling.h"

#pragma unmanaged

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

#define DEFAULT_FILTER_BICUBIC				3.0

#ifndef MIN
#define MIN(a,b) ((a)<(b)?(a):(b))
#endif
#define MIN3(a,b,c) ((a)<(b)?(MIN(a,c)):(MIN(b,c)))
#ifndef MAX
#define MAX(a,b) ((a)<(b)?(b):(a))
#endif
#define MAX3(a,b,c) ((a)<(b)?(MAX(b,c)):(MAX(a,c)))

#define DEFAULT_FILTER_BICUBIC				3.0
#define DEFAULT_FILTER_BOX					0.5
#define DEFAULT_FILTER_GENERALIZED_CUBIC	0.5
#define DEFAULT_FILTER_RADIUS				1.0
#define DEFAULT_LANCZOS8_RADIUS				8.0
#define DEFAULT_LANCZOS3_RADIUS				3.0
#define DEFAULT_HERMITE_RADIUS				1.0
#define DEFAULT_BOX_RADIUS					0.5
#define DEFAULT_TRIANGLE_RADIUS				1.0
#define DEFAULT_BELL_RADIUS					1.5
#define DEFAULT_CUBICSPLINE_RADIUS			2.0
#define DEFAULT_MITCHELL_RADIUS				2.0
#define DEFAULT_COSINE_RADIUS				1.0
#define DEFAULT_CATMULLROM_RADIUS			2.0
#define DEFAULT_QUADRATIC_RADIUS			1.5
#define DEFAULT_QUADRATICBSPLINE_RADIUS		1.5
#define DEFAULT_CUBICCONVOLUTION_RADIUS		3.0
#define DEFAULT_GAUSSIAN_RADIUS				1.0
#define DEFAULT_HANNING_RADIUS				1.0
#define DEFAULT_HAMMING_RADIUS				1.0
#define DEFAULT_SINC_RADIUS					1.0

#define DEFAULT_WELSH_RADIUS				1.0
#define gdAlphaMax 127
#define gdAlphaOpaque 0
#define gdAlphaTransparent 127
#define gdRedMax 255
#define gdGreenMax 255
#define gdBlueMax 255
#define gdTrueColorGetAlpha(c) (((c) & 0x7F000000) >> 24)
#define gdTrueColorGetRed(c) (((c) & 0xFF0000) >> 16)
#define gdTrueColorGetGreen(c) (((c) & 0x00FF00) >> 8)
#define gdTrueColorGetBlue(c) ((c) & 0x0000FF)
#define gdEffectReplace 0
#define gdEffectAlphaBlend 1
#define gdEffectNormal 2
#define gdEffectOverlay 3

#define gdEffectMultiply 4
#define NULL 0


typedef enum {
	HORIZONTAL,
	VERTICAL,
} gdAxis;


#define CLAMP(x, low, high)  (((x) > (high)) ? (high) : (((x) < (low)) ? (low) : (x)))

/* only used here, let do a generic fixed point integers later if required by other
part of GD */
typedef long gdFixed;
/* Integer to fixed point */
#define gd_itofx(x) ((x) << 8)

/* Float to fixed point */
#define gd_ftofx(x) (long)((x) * 256)

/*  Double to fixed point */
#define gd_dtofx(x) (long)((x) * 256)

/* Fixed point to integer */
#define gd_fxtoi(x) ((x) >> 8)

/* Fixed point to float */
# define gd_fxtof(x) ((float)(x) / 256)

/* Fixed point to double */
#define gd_fxtod(x) ((double)(x) / 256)

/* Multiply a fixed by a fixed */
#define gd_mulfx(x,y) (((x) * (y)) >> 8)

/* Divide a fixed by a fixed */
#define gd_divfx(x,y) (((x) << 8) / (y))

typedef struct
{
	double *Weights;  /* Normalized weights of neighboring pixels */
	int Left, Right;   /* Bounds of source pixels window */
} ContributionType;  /* Contirbution information for a single pixel */

typedef struct
{
	ContributionType *ContribRow; /* Row (or column) of contribution weights */
	unsigned int WindowSize,      /* Filter window size (of affecting source pixels) */
		LineLength;      /* Length of line (no. or rows / cols) */
} LineContribType;

#define gdTrueColor(r, g, b) (((r) << 16) + \
			      ((g) << 8) +  \
			      (b))

/* Returns a truecolor value with an alpha channel component.
gdAlphaMax (127, **NOT 255**) is transparent, 0 is completely
opaque. */

#define gdTrueColorAlpha(r, g, b, a) (((a) << 24) + \
				      ((r) << 16) + \
				      ((g) << 8) +  \
				      (b))

/* Convert a double to an unsigned char, rounding to the nearest
* integer and clamping the result between 0 and max.  The absolute
* value of clr must be less than the maximum value of an unsigned
* short. */
static inline unsigned char
uchar_clamp(double clr, unsigned char max) {
	unsigned short result;

	//assert(fabs(clr) <= SHRT_MAX);

	/* Casting a negative float to an unsigned short is undefined.
	* However, casting a float to a signed truncates toward zero and
	* casting a negative signed value to an unsigned of the same size
	* results in a bit-identical value (assuming twos-complement
	* arithmetic).	 This is what we want: all legal negative values
	* for clr will be greater than 255. */

	/* Convert and clamp. */
	result = (unsigned short)(short)(clr + 0.5);
	if (result > max) {
		result = (clr < 0) ? 0 : max;
	}/* if */

	return result;
}/* uchar_clamp*/


/**
* Bicubic interpolation kernel (a=-1):
\verbatim
/
| 1-2|t|**2+|t|**3          , if |t| < 1
h(t) = | 4-8|t|+5|t|**2-|t|**3     , if 1<=|t|<2
| 0                         , otherwise
\
\endverbatim
* ***bd*** 2.2004
*/
typedef enum {
	GD_DEFAULT = 0,
	GD_BELL,
	GD_BESSEL,
	GD_BILINEAR_FIXED,
	GD_BICUBIC,
	GD_BICUBIC_FIXED,
	GD_BLACKMAN,
	GD_BOX,
	GD_BSPLINE,
	GD_CATMULLROM,
	GD_GAUSSIAN,
	GD_GENERALIZED_CUBIC,
	GD_HERMITE,
	GD_HAMMING,
	GD_HANNING,
	GD_MITCHELL,
	GD_NEAREST_NEIGHBOUR,
	GD_POWER,
	GD_QUADRATIC,
	GD_SINC,
	GD_TRIANGLE,
	GD_WEIGHTED4,
	GD_METHOD_COUNT = 21
} gdInterpolationMethod;

/* define struct with name and func ptr and add it to gdImageStruct gdInterpolationMethod interpolation; */

/* Interpolation function ptr */
typedef double(*interpolation_method)(double);

void * gdCalloc(size_t nmemb, size_t size)
{
	return calloc(nmemb, size);
}

void *
gdMalloc(size_t size)
{
	return malloc(size);
}

void *
gdRealloc(void *ptr, size_t size)
{
	return realloc(ptr, size);
}

 void gdFree(void *ptr)
{
	free(ptr);
}

void *
gdReallocEx(void *ptr, size_t size)
{
	void *newPtr = gdRealloc(ptr, size);
	if (!newPtr && ptr)
		gdFree(ptr);
	return newPtr;
}



int overflow2(int a, int b)
{
	if (a <= 0 || b <= 0) {
		//gd_error_ex(GD_WARNING, "one parameter to a memory allocation multiplication is negative or zero, failing operation gracefully\n");
		return 1;
	}
	if (a > INT_MAX / b) {
		//gd_error_ex(GD_WARNING, "product of memory allocation multiplication would exceed INT_MAX, failing operation gracefully\n");
		return 1;
	}
	return 0;
}


static gdImagePtr gdImageCreateTrueColor(int sx, int sy)
{
	int i;
	gdImagePtr im;

	if (overflow2(sx, sy)) {
		return NULL;
	}

	if (overflow2(sizeof(int *), sy)) {
		return 0;
	}

	if (overflow2(sizeof(int), sx)) {
		return NULL;
	}

	im = (gdImage *)gdMalloc(sizeof(gdImage));
	if (!im) {
		return 0;
	}
	memset(im, 0, sizeof(gdImage));

	im->tpixels = (int **)gdMalloc(sizeof(int *) * sy);
	if (!im->tpixels) {
		gdFree(im);
		return 0;
	}
	for (i = 0; (i < sy); i++) {
		im->tpixels[i] = (int *)gdCalloc(sx, sizeof(int));
		if (!im->tpixels[i]) {
			/* 2.0.34 */
			i--;
			while (i >= 0) {
				gdFree(im->tpixels[i]);
				i--;
			}
			gdFree(im->tpixels);
			gdFree(im);
			return 0;
		}
	}
	im->sx = sx;
	im->sy = sy;

	im->trueColor = 1;
	/* 2.0.2: alpha blending is now on by default, and saving of alpha is
	off by default. This allows font antialiasing to work as expected
	on the first try in JPEGs -- quite important -- and also allows
	for smaller PNGs when saving of alpha channel is not really
	desired, which it usually isn't! */
	im->saveAlphaFlag = 0;
	im->alphaBlendingFlag = 1;

	return im;
}


static gdImagePtr gdImageClone(gdImagePtr src) {
	gdImagePtr dst;
	register int i, x;

	dst = gdImageCreateTrueColor(src->sx, src->sy);
	

	if (dst == NULL) {
		return NULL;
	}

	for (i = 0; i < src->sy; i++) {
		for (x = 0; x < src->sx; x++) {
			dst->tpixels[i][x] = src->tpixels[i][x];
		}
	}
	

	dst->alphaBlendingFlag = src->alphaBlendingFlag;
	dst->saveAlphaFlag = src->saveAlphaFlag;

	
	return dst;
}



static double filter_bicubic(const double t)
{
	const double abs_t = (double)fabs(t);
	const double abs_t_sq = abs_t * abs_t;
	if (abs_t<1) return 1 - 2 * abs_t_sq + abs_t_sq*abs_t;
	if (abs_t<2) return 4 - 8 * abs_t + 5 * abs_t_sq - abs_t_sq*abs_t;
	return 0;
}


static inline LineContribType * _gdContributionsAlloc(unsigned int line_length, unsigned int windows_size)
{
	unsigned int u = 0;
	LineContribType *res;

	res = (LineContribType *)gdMalloc(sizeof(LineContribType));
	if (!res) {
		return NULL;
	}
	res->WindowSize = windows_size;
	res->LineLength = line_length;
	res->ContribRow = (ContributionType *)gdMalloc(line_length * sizeof(ContributionType));

	for (u = 0; u < line_length; u++) {
		res->ContribRow[u].Weights = (double *)gdMalloc(windows_size * sizeof(double));
	}
	return res;
}

static inline void _gdContributionsFree(LineContribType * p)
{
	unsigned int u;
	for (u = 0; u < p->LineLength; u++)  {
		gdFree(p->ContribRow[u].Weights);
	}
	gdFree(p->ContribRow);
	gdFree(p);
}

static inline LineContribType *_gdContributionsCalc(unsigned int line_size, unsigned int src_size, double scale_d, const interpolation_method pFilter)
{
	double width_d;
	double scale_f_d = 1.0;
	const double filter_width_d = DEFAULT_BOX_RADIUS;
	int windows_size;
	unsigned int u;
	LineContribType *res;

	if (scale_d < 1.0) {
		width_d = filter_width_d / scale_d;
		scale_f_d = scale_d;
	}
	else {
		width_d = filter_width_d;
	}

	windows_size = 2 * (int)ceil(width_d) + 1;
	res = _gdContributionsAlloc(line_size, windows_size);

	for (u = 0; u < line_size; u++) {
		const double dCenter = (double)u / scale_d;
		/* get the significant edge points affecting the pixel */
		register int iLeft = MAX(0, (int)floor(dCenter - width_d));
		int iRight = MIN((int)ceil(dCenter + width_d), (int)src_size - 1);
		double dTotalWeight = 0.0;
		int iSrc;

		/* Cut edge points to fit in filter window in case of spill-off */
		if (iRight - iLeft + 1 > windows_size)  {
			if (iLeft < ((int)src_size - 1 / 2))  {
				iLeft++;
			}
			else {
				iRight--;
			}
		}

		res->ContribRow[u].Left = iLeft;
		res->ContribRow[u].Right = iRight;

		for (iSrc = iLeft; iSrc <= iRight; iSrc++) {
			dTotalWeight += (res->ContribRow[u].Weights[iSrc - iLeft] = scale_f_d * (*pFilter)(scale_f_d * (dCenter - (double)iSrc)));
		}

		if (dTotalWeight < 0.0) {
			_gdContributionsFree(res);
			return NULL;
		}

		if (dTotalWeight > 0.0) {
			for (iSrc = iLeft; iSrc <= iRight; iSrc++) {
				res->ContribRow[u].Weights[iSrc - iLeft] /= dTotalWeight;
			}
		}
	}
	return res;
}


static inline void
_gdScaleOneAxis(gdImagePtr pSrc, gdImagePtr dst,
unsigned int dst_len, unsigned int row, LineContribType *contrib,
gdAxis axis)
{
	unsigned int ndx;

	for (ndx = 0; ndx < dst_len; ndx++) {
		double r = 0, g = 0, b = 0, a = 0;
		const int left = contrib->ContribRow[ndx].Left;
		const int right = contrib->ContribRow[ndx].Right;
		int *dest = (axis == HORIZONTAL) ?
			&dst->tpixels[row][ndx] :
			&dst->tpixels[ndx][row];

		int i;

		/* Accumulate each channel */
		for (i = left; i <= right; i++) {
			const int left_channel = i - left;
			const int srcpx = (axis == HORIZONTAL) ?
				pSrc->tpixels[row][i] :
				pSrc->tpixels[i][row];

			r += contrib->ContribRow[ndx].Weights[left_channel]
				* (double)(gdTrueColorGetRed(srcpx));
			g += contrib->ContribRow[ndx].Weights[left_channel]
				* (double)(gdTrueColorGetGreen(srcpx));
			b += contrib->ContribRow[ndx].Weights[left_channel]
				* (double)(gdTrueColorGetBlue(srcpx));
			a += contrib->ContribRow[ndx].Weights[left_channel]
				* (double)(gdTrueColorGetAlpha(srcpx));
		}/* for */

		*dest = gdTrueColorAlpha(uchar_clamp(r, 0xFF), uchar_clamp(g, 0xFF),
			uchar_clamp(b, 0xFF),
			uchar_clamp(a, 0x7F)); /* alpha is 0..127 */
	}/* for */
}/* _gdScaleOneAxis*/




static inline int _gdScalePass(const gdImagePtr pSrc, const unsigned int src_len,
	const gdImagePtr pDst, const unsigned int dst_len,
	const unsigned int num_lines,
	const gdAxis axis)
{
	unsigned int line_ndx;
	LineContribType * contrib;

	/* Same dim, just copy it. */
	//assert(dst_len != src_len); // TODO: caller should handle this.

	contrib = _gdContributionsCalc(dst_len, src_len,
		(double)dst_len / (double)src_len,
		filter_bicubic);
	if (contrib == NULL) {
		return 0;
	}

	/* Scale each line */
	for (line_ndx = 0; line_ndx < num_lines; line_ndx++) {
		_gdScaleOneAxis(pSrc, pDst, dst_len, line_ndx, contrib, axis);
	}
	_gdContributionsFree(contrib);

	return 1;
}/* _gdScalePass*/


static gdImagePtr
gdImageScaleTwoPass(const gdImagePtr src, const unsigned int new_width,
const unsigned int new_height)
{
	const unsigned int src_width = src->sx;
	const unsigned int src_height = src->sy;
	gdImagePtr tmp_im = NULL;
	gdImagePtr dst = NULL;

	/* First, handle the trivial case. */
	if (src_width == new_width && src_height == new_height) {
		return gdImageClone(src);
	}/* if */

	/* Scale horizontally unless sizes are the same. */
	if (src_width == new_width) {
		tmp_im = src;
	}
	else {
		tmp_im = gdImageCreateTrueColor(new_width, src_height);
		if (tmp_im == NULL) {
			return NULL;
		}
		_gdScalePass(src, src_width, tmp_im, new_width, src_height, HORIZONTAL);
	}/* if .. else*/

	/* If vertical sizes match, we're done. */
	if (src_height == new_height) {
		//assert(tmp_im != src);
		return tmp_im;
	}/* if */

	/* Otherwise, we need to scale vertically. */
	dst = gdImageCreateTrueColor(new_width, new_height);
	if (dst != NULL) {
		_gdScalePass(tmp_im, src_height, dst, new_height, new_width, VERTICAL);
	}/* if */

	if (src != tmp_im) {
		gdFree(tmp_im);
	}/* if */

	return dst;
}/* gdImageScaleTwoPass*/
static void gdImageDestroy(gdImagePtr im)
{
	int i;

	if (im->tpixels) {
		for (i = 0; (i < im->sy); i++) {
			gdFree(im->tpixels[i]);
		}
		gdFree(im->tpixels);
	}
	gdFree(im);
}


#pragma managed


using namespace System;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;
using namespace ImageResizer::Resizing;

namespace ImageResizer{
	namespace Plugins{
		namespace FastScaling {

			public ref class BitmapScaler
			{
			public:
				void ScaleBitmap(Bitmap^ source, Bitmap^ dest, Rectangle crop, Rectangle target){
					gdImagePtr gdSource;
					gdImagePtr gdResult;
					try{
						gdSource = BitmapToGd(source, crop);
						gdResult = gdImageScaleTwoPass(gdSource, target.Width, target.Height);
						CopyGdToBitmap(gdResult, dest, target);
					}finally{
						if (gdSource != 0) {
							gdImageDestroy(gdSource);
							gdSource = 0;
						}
						if (gdResult != 0){
							gdImageDestroy(gdResult);
							gdResult = 0;
						}

					}
				}

			private:

				void CopyGdToBitmap(gdImagePtr source, Bitmap^ target, Rectangle targetArea){
					if (target->PixelFormat != PixelFormat::Format32bppArgb){
						throw gcnew ArgumentOutOfRangeException("target", "Invalid pixel format " + target->PixelFormat.ToString());
					}
					BitmapData ^sourceData;
					try{
						sourceData = target->LockBits(targetArea, ImageLockMode::ReadOnly, target->PixelFormat);
						int sy = source->sy;
						int sx = source->sx;
						int i;
						IntPtr^ scan0intptr = sourceData->Scan0;
						void *scan0 = scan0intptr->ToPointer();
						for (i = 0; (i < sy); i++) {
							void * linePtr = (void *)((long)scan0 + (sourceData->Stride * i) + (targetArea.Left * 4));
							memcpy(linePtr, source->tpixels[i], sx * 4);
						}
					}
					finally{
						target->UnlockBits(sourceData);
					}
				}

				gdImagePtr BitmapToGd(Bitmap^ source, Rectangle from){
					int i;
					int j;
					bool hasAlpha = source->PixelFormat == PixelFormat::Format32bppArgb;
					if (source->PixelFormat != PixelFormat::Format32bppArgb && source->PixelFormat != PixelFormat::Format24bppRgb){
						throw gcnew ArgumentOutOfRangeException("source", "Invalid pixel format " + source->PixelFormat.ToString());
					}
					if (from.X < 0 || from.Y < 0 || from.Right > source->Width || from.Bottom > source->Height) {
						throw gcnew ArgumentOutOfRangeException("from");
					}
					int sx = from.Width;
					int sy = from.Height;

					int mask = ((INT_MAX >> 8) << 8);
					gdImagePtr im;
					im = (gdImage *)gdMalloc(sizeof(gdImage));
					if (!im) {
						return 0;
					}
					memset(im, 0, sizeof(gdImage));

					im->tpixels = (int **)gdMalloc(sizeof(int *) * sy);
					if (!im->tpixels) {
						gdFree(im);
						return 0;
					}
					BitmapData ^sourceData;
					try{
						sourceData = source->LockBits(from, ImageLockMode::ReadWrite, source->PixelFormat);

						for (i = 0; (i < sy); i++) {
							im->tpixels[i] = (int *)gdCalloc(sx, 4);
							if (!im->tpixels[i]) {
								/* 2.0.34 */
								i--;
								while (i >= 0) {
									gdFree(im->tpixels[i]);
									i--;
								}
								gdFree(im->tpixels);
								gdFree(im);
								return 0;
							}
							else{
								IntPtr^ scan0intptr = sourceData->Scan0;

								void *scan0 = scan0intptr->ToPointer();
								void * linePtr = (void *)((long)scan0 + (sourceData->Stride * i) + (from.Left * (hasAlpha ? 4 : 3)));
								if (hasAlpha){
									memcpy(im->tpixels[i], linePtr, sx * 4);
								}
								else{
									for (j = 0; j < sx; j++){

										im->tpixels[i][j] = *(int *)((long)linePtr + (j * 3)) & mask;
									}
								}
							}

						}
					}
					finally{
						source->UnlockBits(sourceData);
					}
					im->sx = sx;
					im->sy = sy;

					im->trueColor = 1;
					/* 2.0.2: alpha blending is now on by default, and saving of alpha is
					off by default. This allows font antialiasing to work as expected
					on the first try in JPEGs -- quite important -- and also allows
					for smaller PNGs when saving of alpha channel is not really
					desired, which it usually isn't! */
					im->saveAlphaFlag = 0;
					im->alphaBlendingFlag = 1;

					return im;
				}
			};


			public ref class FastScalingPlugin : public ImageResizer::Resizing::BuilderExtension, IPlugin
			{
			protected:
				virtual RequestedAction InternalGraphicsDrawImage(ImageState^ s, Bitmap^ dest, Bitmap^ source, array<PointF>^ targetArea, RectangleF sourceArea, ImageAttributes^ imageAttributes) override{
				/*	System::Collections::Specialized::NameValueCollection^ query = safe_cast<System::Collections::Specialized::NameValueCollection^>(s->settings);


					String^ fastScale = query->Get("fastscale");
					String^ sTrue = "true";
					if (fastScale != sTrue){
						return RequestedAction::None;
					}*/
					RectangleF targetBox = ImageResizer::Util::PolygonMath::GetBoundingBox(targetArea);
					if (targetBox.Location != targetArea[0] || targetBox.Width != (targetArea[1].X - targetArea[0].X)){
						return RequestedAction::None;
					}
					BitmapScaler ^scaler = gcnew BitmapScaler();
					scaler->ScaleBitmap(source, dest, Util::PolygonMath::ToRectangle(sourceArea), Util::PolygonMath::ToRectangle(targetBox));
					return RequestedAction::Cancel;
					
				}
			public: 
				virtual ImageResizer::Plugins::IPlugin^ Install(ImageResizer::Configuration::Config^ c) override{
					c->Plugins->add_plugin(this);
					return this;
				}
				virtual bool Uninstall(ImageResizer::Configuration::Config^ c) override{
					c->Plugins->remove_plugin(this);
					return true;
				}
			};
			
		}
	}
