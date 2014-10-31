// This is the main DLL file.

#include "stdafx.h"
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include "math.h"
#include "ImageResizer.Plugins.FastScaling.h"

#pragma unmanaged


//GDI+ uses BGR/BGRA byte order.
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
	unsigned int **tpixels;
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
#define gdTrueColorGetAlpha(c) (((c) & 0xFF000000) >> 24)
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
	float *Weights;  /* Normalized weights of neighboring pixels */
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

/* Convert a float to an unsigned char, rounding to the nearest
* integer and clamping the result between 0 and max.  The absolute
* value of clr must be less than the maximum value of an unsigned
* short. */
static inline unsigned char
uchar_clamp(float clr, unsigned char max) {
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

	im->tpixels = (unsigned int **)gdMalloc(sizeof(unsigned int *)* sy);

	if (!im->tpixels) {
		gdFree(im);
		return 0;
	}

	im->tpixels[0] = (unsigned int *)gdCalloc(sx*sy, sizeof(unsigned int));

	for (i = 1; (i < sy); i++)
		im->tpixels[i] = &im->tpixels[0][i*sx];

	if (!im->tpixels[0]) {
		gdFree(im->tpixels);
		gdFree(im);
		return 0;
	}/**/

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

static void gdImageDestroy(gdImagePtr im)
{
    int i;

    if (im->tpixels) {
		gdFree(im->tpixels[0]);
        gdFree(im->tpixels);
    }
    gdFree(im);
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

	return res;
}

static inline void _gdContributionsFree(LineContribType * p)
{
	unsigned int u;
    float * last = 0;
	for (u = 0; u < p->LineLength; u++)  {
        if (last != p->ContribRow[u].Weights){
            gdFree(p->ContribRow[u].Weights);
        }
        last = p->ContribRow[u].Weights;
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

        if (u > 0 && res->ContribRow[u - 1].Right - res->ContribRow[u - 1].Left == iRight - iLeft){
            res->ContribRow[u].Weights = res->ContribRow[u - 1].Weights;
        }
        else{
            res->ContribRow[u].Weights = (float *)gdMalloc(windows_size * sizeof(float));
            

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
	}
	return res;
}


static inline void
_gdScale(float *source_buffer, unsigned int source_buffer_count, unsigned int source_buffer_len,
float *dest_buffer, unsigned int dest_buffer_count, unsigned int dest_buffer_len, ContributionType * weights){

    unsigned int ndx;
    for (ndx = 0; ndx < dest_buffer_count; ndx++) {
        float r = 0, g = 0, b = 0, a = 0;
        const int left = weights[ndx].Left;
        const int right = weights[ndx].Right;

        const float * weightArray = weights[ndx].Weights;
        int i;

        /* Accumulate each channel */
        for (i = left; i <= right; i++) {
            const float weight = weightArray[i - left];

            a += weight * source_buffer[i * 4];
            r += weight * source_buffer[i * 4 + 1];
            g += weight * source_buffer[i * 4 + 2];
            b += weight * source_buffer[i * 4 + 3];
        }

        dest_buffer[ndx * 4] = a;
        dest_buffer[ndx * 4 + 1] = r;
        dest_buffer[ndx * 4 + 2] = g;
        dest_buffer[ndx * 4 + 3] = b;
    }

}


static inline void _gdScaleXAndPivotRow(unsigned char * source_row, unsigned int source_pixel_count, ContributionType * weights, gdImagePtr dest, unsigned int dest_column_index, float *source_buffer, unsigned int source_buffer_len, float *dest_buffer, unsigned int dest_buffer_len, float *lut){

    unsigned int bix;

    for (bix = 0; bix < source_buffer_len; bix++){
        source_buffer[bix] = lut[source_row[bix]];
    }

    _gdScale(source_buffer, source_pixel_count, source_buffer_len, dest_buffer, dest->sy, dest_buffer_len, weights);



    for (bix = 0; bix < dest->sy; bix++){
        dest->tpixels[bix][dest_column_index] = gdTrueColorAlpha(
            uchar_clamp(dest_buffer[bix * 4], 0xFF),
            uchar_clamp(dest_buffer[bix * 4 + 1], 0xFF),
            uchar_clamp(dest_buffer[bix * 4 + 2], 0xFF),
            uchar_clamp(dest_buffer[bix * 4 + 3], 0xFF)); /* alpha is 0..256 */
    }

}




static inline void _gdScaleXAndPivotRowUnbuffered(unsigned char * source_row, unsigned int source_pixel_count, ContributionType * weights, gdImagePtr dest, unsigned int dest_column_index, float *lut){

    unsigned int dest_count = dest->sy;

    unsigned int ndx;
    for (ndx = 0; ndx < dest_count; ndx++) {
        int r = 0, g = 0, b = 0, a = 0;
        const int left = weights[ndx].Left;
        const int right = weights[ndx].Right;

        const float * weightArray = weights[ndx].Weights;
        int i;

        /* Accumulate each channel */
        for (i = left; i <= right; i++) {
            const float weight = weightArray[i - left];

            a += weight *  lut[source_row[i * 4]];
            r += weight * lut[source_row[i * 4 + 1]];
            g += weight * lut[source_row[i * 4 + 2]];
            b += weight * lut[source_row[i * 4 + 3]];
        }

        dest->tpixels[ndx][dest_column_index] = gdTrueColorAlpha(
            uchar_clamp(a, 0xFF),
            uchar_clamp(r, 0xFF),
            uchar_clamp(g, 0xFF),
            uchar_clamp(b, 0xFF));
    }

}



static inline int _gdScaleXAndPivot(const gdImagePtr pSrc,
    const gdImagePtr pDst, float *lut)
{
    unsigned int line_ndx;
    LineContribType * contrib;
    /* Same dim, just copy it. */
    //assert(dst_len != src_len); // TODO: caller should handle this.


    contrib = _gdContributionsCalc(pDst->sy, pSrc->sx,
        (double)pDst->sy / (double)pSrc->sx,
        filter_bicubic);
    if (contrib == NULL) {
        return 0;
    }

    int buffer = 1;

    if (buffer == 1){
        unsigned int source_buffer_len = pSrc->sx * 4;
        float *sourceBuffer = (float *)gdMalloc(sizeof(float) * source_buffer_len);

        unsigned int dest_buffer_len = pDst->sy * 4;
        float *destBuffer = (float *)gdMalloc(sizeof(float) * dest_buffer_len);

        /* Scale each line */
        for (line_ndx = 0; line_ndx < pSrc->sy; line_ndx++) {
            _gdScaleXAndPivotRow((unsigned char *)(pSrc->tpixels[line_ndx]), pSrc->sx, contrib->ContribRow, pDst, line_ndx,
                sourceBuffer, source_buffer_len, destBuffer, dest_buffer_len, lut);
        }
        gdFree(sourceBuffer);
        gdFree(destBuffer);
    }
    else{
        for (line_ndx = 0; line_ndx < pSrc->sy; line_ndx++) {
            _gdScaleXAndPivotRowUnbuffered((unsigned char *)(pSrc->tpixels[line_ndx]), pSrc->sx, contrib->ContribRow, pDst, line_ndx, lut);
        }
    }
    _gdContributionsFree(contrib);

    return 1;
}/* _gdScalePass*/


static gdImagePtr
gdImageScaleTwoPass(const gdImagePtr src, const unsigned int new_width,
const unsigned int new_height)
{
    gdImagePtr tmp_im = NULL;
    gdImagePtr dst = NULL;


    float lut[256];
    for (int n = 0; n < 256; n++) lut[n] = (float)n;

    /* Scale horizontally  */
    tmp_im = gdImageCreateTrueColor(src->sy, new_width);
    if (tmp_im == NULL) {
        return NULL;
    }
    _gdScaleXAndPivot(src, tmp_im, lut);


    /* Otherwise, we need to scale vertically. */
    dst = gdImageCreateTrueColor(new_width, new_height);
    if (dst != NULL) {
        _gdScaleXAndPivot(tmp_im, dst, lut);
    }

    if (src != tmp_im) {
        gdImageDestroy(tmp_im);
    }

    return dst;
}/* gdImageScaleTwoPass*/


static void unpack24bitRow(int width, void * sourceLine, unsigned int * destArray){
	for (int i = 0; i < width; i++){
        destArray[i] = (*(unsigned int *)((unsigned long  long)sourceLine + (i * 3))) | 0xFF000000;
	}
}




#pragma managed


using namespace System;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;
using namespace ImageResizer::Resizing;
using namespace System::Diagnostics;
namespace ImageResizer{
	namespace Plugins{
		namespace FastScaling {

			public ref class BitmapScaler
			{
			public:
				void ScaleBitmap(Bitmap^ source, Bitmap^ dest, Rectangle crop, Rectangle target, IProfiler^ p){
					gdImagePtr gdSource;
					gdImagePtr gdResult;
					try{
                        p->Start("BitmapToGd",false);
						gdSource = BitmapToGd(source, crop);
                        p->Stop("BitmapToGd", true, false);
                        p->Start("Scale", false);
						gdResult = Scale(gdSource, target.Width, target.Height, p);
                        p->Stop("Scale", true, false);
                        p->Start("CopyGdToBitmap", false);
						CopyGdToBitmap(gdResult, dest, target);
                        p->Stop("CopyGdToBitmap", true, false);
                        p->Start("GdDispose", false);
					}finally{
						if (gdSource != 0) {
							gdImageDestroy(gdSource);
							gdSource = 0;
						}
						if (gdResult != 0){
							gdImageDestroy(gdResult);
							gdResult = 0;
						}
                        p->Stop("GdDispose", true, false);

					}
				}
				 

			private:

                gdImagePtr Scale(gdImagePtr source, int width, int height, IProfiler^ p){

                    gdImagePtr tmp_im = NULL;
                    gdImagePtr dst = NULL;


                    float lut[256];
                    for (int n = 0; n < 256; n++) lut[n] = (float)n;

                    p->Start("create temp image(sy x dx)", false);
                    /* Scale horizontally  */
                    tmp_im = gdImageCreateTrueColor(source->sy, width);
                   
                    if (tmp_im == NULL) {
                        return NULL;
                    }
                    try{
                        p->Stop("create temp image(sy x dx)", true, false);

                        p->Start("scale and pivot to temp", false);
                        _gdScaleXAndPivot(source, tmp_im, lut);
                        p->Stop("scale and pivot to temp", true, false);

                        p->Start("create image(dx x dy)", false);
                        /* Otherwise, we need to scale vertically. */
                        dst = gdImageCreateTrueColor(width, height);
                        p->Stop("create image(dx x dy)", true, false);
                        if (dst == NULL) {
                            return NULL;
                        }

                        p->Start("scale and pivot to final", false);
                        _gdScaleXAndPivot(tmp_im, dst, lut);
                        p->Stop("scale and pivot to final", true, false);
                    }
                    finally{
                        p->Start("destroy temp image", false);
                        gdImageDestroy(tmp_im);
                        p->Stop("destroy temp image", true, false);
                    }
                    return dst;
				}

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
                            void * linePtr = (void *)((unsigned long  long)scan0 + (sourceData->Stride * i) + (targetArea.Left * 4));
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
					gdImagePtr im = gdImageCreateTrueColor(sx, sy);
					
					BitmapData ^sourceData;
					try{
						sourceData = source->LockBits(from, ImageLockMode::ReadWrite, source->PixelFormat);

						for (i = 0; (i < sy); i++) {
							IntPtr^ scan0intptr = sourceData->Scan0;

							void *scan0 = scan0intptr->ToPointer();
							void *linePtr = (void *)((unsigned long long)scan0 + (unsigned long  long)((sourceData->Stride * i) + (from.Left * (hasAlpha ? 4 : 3))));
							if (hasAlpha){
								memcpy(im->tpixels[i], linePtr, sx * 4);
							}
							else{
								unpack24bitRow(sx, linePtr, im->tpixels[i]);
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
                    scaler->ScaleBitmap(source, dest, Util::PolygonMath::ToRectangle(sourceArea), Util::PolygonMath::ToRectangle(targetBox), s->Job->Profiler);
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
}