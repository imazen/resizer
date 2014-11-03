// This is the main DLL file.

#include "stdafx.h"
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include "math.h"
#include "ImageResizer.Plugins.FastScaling.h"

#pragma unmanaged

typedef struct BitmapBgraStruct{
    int w;
    int h;
    int stride;
    unsigned char *pixels;
    unsigned int *pixelInts;
    int hasAlpha;
    int ownMem;
    int bpp;
} BitmapBgra;

typedef BitmapBgra *BitmapBgraPtr;

typedef struct InterpolationDetailsStruct *InterpolationDetailsPtr;

typedef double(*detailed_interpolation_method)(InterpolationDetailsPtr, double);

typedef struct InterpolationDetailsStruct{
    double window;
    double * cubic_coefficients;
    double blur;
    detailed_interpolation_method filter;
    int use_halving;
    int allow_source_mutation;
    int post_resize_sharpen_percent;
    int filter_var_a;
}InterpolationDetails;


#ifndef MIN
#define MIN(a,b) ((a)<(b)?(a):(b))
#endif
#define MIN3(a,b,c) ((a)<(b)?(MIN(a,c)):(MIN(b,c)))
#ifndef MAX
#define MAX(a,b) ((a)<(b)?(b):(a))
#endif
#define MAX3(a,b,c) ((a)<(b)?(MAX(b,c)):(MAX(a,c)))

#define NULL 0

#define CLAMP(x, low, high)  (((x) > (high)) ? (high) : (((x) < (low)) ? (low) : (x)))

#define DEFAULT_BOX_RADIUS					0.5

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

static inline unsigned char
uchar_clamp_ff(float clr) {
    unsigned short result;
    result = (unsigned short)(short)(clr + 0.5);
    if (result > 255) {
        result = (clr < 0) ? 0 : 255;
    }

    return result;
}




static int overflow2(int a, int b)
{
	if (a <= 0 || b <= 0) {
		return 1;
	}
	if (a > INT_MAX / b) {
		return 1;
	}
	return 0;
}


static BitmapBgraPtr CreateBitmapBgraPtr(int sx, int sy, int zeroed, int alloc=1, int bpp=4)
{
	int i;
    BitmapBgraPtr im;

    if (overflow2(sx, sy) || overflow2(sizeof(int *), sy) || overflow2(sizeof(int), sx)) {
		return NULL;
	}


    im = (BitmapBgra *)malloc(sizeof(BitmapBgra));
	if (!im) {
		return 0;
	}
    memset(im, 0, sizeof(BitmapBgra));
    im->w = sx;
    im->h = sy;
    im->bpp = bpp;
    im->stride = sx * bpp;
	
    if (alloc)
    {
        im->ownMem = 1;
        if (zeroed){
            im->pixels = (unsigned char *)calloc(sy * im->stride, sizeof(unsigned char));
        }
        else{
            im->pixels = (unsigned char *)malloc(sy * im->stride);
        }
        im->pixelInts = (unsigned int *)im->pixels;

        if (!im->pixels) {
            free(im);
            return 0;
        }
    }
    else
        im->ownMem = 0;

	return im;
}


static void DestroyBitmapBgra(BitmapBgraPtr im)
{
    int i;
    if (im->pixels && im->ownMem) {
		free(im->pixels);
    }
    free(im);
}


static double * derive_cubic_coefficients(double B, double C){
    double bx2 = B + B;
    double co[7] = { 1.0 - (1.0 / 3.0)*B,
        -3.0 + bx2 + C,
        2.0 - 1.5*B - C,
        (4.0 / 3.0)*B + 4.0*C,
        -8.0*C - bx2,
        B + 5.0*C,
        (-1.0 / 6.0)*B - C };
    return co;
}


static inline double filter_flex_cubic(const InterpolationDetailsPtr d, const double x)
{
    const double t = (double)fabs(x) / d->blur;


    const double * co = d->cubic_coefficients;
    if (t < 1.0){
        return (co[0] + t * (t* (co[1] + t*co[2])));
    }
    if (t < 2.0){
        return(co[3] + t*(co[4] + t* (co[5] + t*co[6])));
    }
    return(0.0);
}
static inline double filter_bicubic_fast(const InterpolationDetailsPtr d, const double t)
{
    const double abs_t = (double)fabs(t) / d->blur;
    const double abs_t_sq = abs_t * abs_t;
    if (abs_t<1) return 1 - 2 * abs_t_sq + abs_t_sq*abs_t;
    if (abs_t<2) return 4 - 8 * abs_t + 5 * abs_t_sq - abs_t_sq*abs_t;
    return 0;
}

#define IR_PI  double (3.1415926535897932384626433832795)
#define IR_SINC(value) (value == 0 ? 1 : sin(value * IR_PI) / (value * IR_PI))

static inline double filter_lanczos(const InterpolationDetailsPtr d, const double t)
{
    const double width = d->filter_var_a;

    const double abs_t = (double)fabs(t) / d->blur;
    if (abs_t < width)	{
        return (IR_SINC(abs_t) * IR_SINC(abs_t / width));
    }
    return 0;
}





static InterpolationDetailsPtr CreateBicubicCustom(double window, double blur, double B, double C){
    InterpolationDetailsPtr d = (InterpolationDetails *)malloc(sizeof(InterpolationDetails));
    d->blur = blur;
    d->cubic_coefficients = derive_cubic_coefficients(B,C);
    d->filter = filter_flex_cubic;
    d->window = window;
    return d;
}

static InterpolationDetailsPtr DetailsLanczosCustom(double window, double blur){
    InterpolationDetailsPtr d = (InterpolationDetails *)malloc(sizeof(InterpolationDetails));
    d->blur = blur;
    d->filter = filter_lanczos;
    d->window = window;
    d->filter_var_a = 3;
    return d;
}
static InterpolationDetailsPtr DetailsLanczos(){
    return DetailsLanczosCustom(0.5, 1);
}

static InterpolationDetailsPtr DetailsDefault(){
    return CreateBicubicCustom( 0.5,1, 1, 0);
}

static InterpolationDetailsPtr DetailsGeneralCubic(){
    return CreateBicubicCustom(0.70710678118654752440084436210484903928483593768847, 2, 1, 0);
}
static InterpolationDetailsPtr DetailsCatmullRom(){
    return CreateBicubicCustom(0.70710678118654752440084436210484903928483593768847, 2, 0, 0.5);
}
static InterpolationDetailsPtr DetailsMitchell(){
    return CreateBicubicCustom(0.70710678118654752440084436210484903928483593768847, 8.0 / 7.0, 1. / 3., 1. / 3.);
}
static InterpolationDetailsPtr DetailsRobidoux(){
    return CreateBicubicCustom(0.70710678118654752440084436210484903928483593768847, 1.1685777620836932,
        0.37821575509399867, 0.31089212245300067);
}

static InterpolationDetailsPtr DetailsRobidouxSharp(){
    return CreateBicubicCustom(0.70710678118654752440084436210484903928483593768847, 1.105822933719019,
        0.2620145123990142, 0.3689927438004929);
}
static InterpolationDetailsPtr DetailsHermite(){
    return CreateBicubicCustom(0.70710678118654752440084436210484903928483593768847, 2, 1, 0);
}






static inline LineContribType *  ContributionsAlloc(unsigned int line_length, unsigned int windows_size)
{
	unsigned int u = 0;
	LineContribType *res;

	res = (LineContribType *)malloc(sizeof(LineContribType));
	if (!res) {
		return NULL;
	}
	res->WindowSize = windows_size;
	res->LineLength = line_length;
	res->ContribRow = (ContributionType *)malloc(line_length * sizeof(ContributionType));


    float *allWeights = (float *)malloc(windows_size * line_length * sizeof(float));
    
    for (int i = 0; i < line_length; i++)
        res->ContribRow[i].Weights = allWeights + (i * windows_size);

	return res;
}

static inline void ContributionsFree(LineContribType * p)
{
    free(p->ContribRow[0].Weights);
	free(p->ContribRow);
	free(p);
}

static inline LineContribType *ContributionsCalc(unsigned int line_size, unsigned int src_size, double scale_d, const InterpolationDetailsPtr details)
{
	double width_d;
	double scale_f_d = 1.0;
    const double filter_width_d = details->window;
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
	res = ContributionsAlloc(line_size, windows_size);

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
            dTotalWeight += (res->ContribRow[u].Weights[iSrc - iLeft] = scale_f_d * (*details->filter)(details, scale_f_d * (dCenter - (double)iSrc)));
        }

        if (dTotalWeight < 0.0) {
            ContributionsFree(res);
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
ScaleBgraFloat(float *source_buffer, unsigned int source_buffer_count, unsigned int source_buffer_len,
float *dest_buffer, unsigned int dest_buffer_count, unsigned int dest_buffer_len, ContributionType * weights, int from_step=4, int to_step=4){

    unsigned int ndx;

    // if both have alpha, process it
    if (from_step == 4 && to_step == 4)
    {
        for (ndx = 0; ndx < dest_buffer_count; ndx++) {
            float r = 0, g = 0, b = 0, a = 0;
            const int left = weights[ndx].Left;
            const int right = weights[ndx].Right;

            const float * weightArray = weights[ndx].Weights;
            int i;

            /* Accumulate each channel */
            for (i = left; i <= right; i++) {
                const float weight = weightArray[i - left];

                b += weight * source_buffer[i * from_step];
                g += weight * source_buffer[i * from_step + 1];
                r += weight * source_buffer[i * from_step + 2];
                a += weight * source_buffer[i * from_step + 3];
            }

            dest_buffer[ndx * to_step] = b;
            dest_buffer[ndx * to_step + 1] = g;
            dest_buffer[ndx * to_step + 2] = r;
            dest_buffer[ndx * to_step + 3] = a;
        }
    }
    // otherwise do the same thing without 4th chan
    // (ifs in loops are expensive..)
    else
    {
        for (ndx = 0; ndx < dest_buffer_count; ndx++) {
            float r = 0, g = 0, b = 0;
            const int left = weights[ndx].Left;
            const int right = weights[ndx].Right;

            const float * weightArray = weights[ndx].Weights;
            int i;

            /* Accumulate each channel */
            for (i = left; i <= right; i++) {
                const float weight = weightArray[i - left];

                b += weight * source_buffer[i * from_step];
                g += weight * source_buffer[i * from_step + 1];
                r += weight * source_buffer[i * from_step + 2];
            }

            dest_buffer[ndx * to_step] = b;
            dest_buffer[ndx * to_step + 1] = g;
            dest_buffer[ndx * to_step + 2] = r;
        }
    }

}


static inline void ScaleXAndPivotRows(BitmapBgraPtr source_bitmap, unsigned int start_row, unsigned int row_count,  ContributionType * weights, BitmapBgraPtr dest, float *source_buffers, unsigned int source_buffer_len, float *dest_buffers, unsigned int dest_buffer_len, float *lut){

    register unsigned int row, bix, bufferSet;
    const register unsigned int from_pixel_count = source_bitmap->w;
    const register unsigned int to_pixel_count = dest->h;
    
    for (row = 0; row < row_count; row++)
    {
        unsigned char *src_start = source_bitmap->pixels + (start_row + row)*source_bitmap->stride;
        for (bix = 0; bix < source_buffer_len; bix++)
            source_buffers[row * source_buffer_len + bix] = lut[src_start[bix]];
    }
    
    //Actual scaling seems responsible for about 40% of execution time
    for (bufferSet = 0; bufferSet < row_count; bufferSet++){
        ScaleBgraFloat(source_buffers + (source_buffer_len * bufferSet), from_pixel_count, source_buffer_len, 
            dest_buffers + (dest_buffer_len * bufferSet), to_pixel_count, dest_buffer_len, weights, source_bitmap->bpp, dest->bpp);
    }
    
    // process rgb first
    unsigned char *dst_start = dest->pixels + start_row * dest->bpp;
    int stride_offset = dest->stride - dest->bpp * row_count;

    for (bix = 0; bix < to_pixel_count; bix++){
        int dest_buffer_start = bix * dest->bpp;

        for (bufferSet = 0; bufferSet < row_count; bufferSet++){
            *dst_start = uchar_clamp_ff(dest_buffers[dest_buffer_start]);
            *(dst_start + 1) = uchar_clamp_ff(dest_buffers[dest_buffer_start + 1]);
            *(dst_start + 2) = uchar_clamp_ff(dest_buffers[dest_buffer_start + 2]);

            dest_buffer_start += dest_buffer_len;
            dst_start += dest->bpp;
        }

        dst_start += stride_offset;
    }

    // copy alpha if we had it or 255-fill
    if (dest->bpp == 4)
    {
        dst_start = dest->pixels + start_row * dest->bpp;

        if (source_bitmap->bpp == 4)
        {
            for (bix = 0; bix < to_pixel_count; bix++){
                int dest_buffer_start = bix * dest->bpp;
                for (bufferSet = 0; bufferSet < row_count; bufferSet++){
                    *(dst_start + 3) = uchar_clamp_ff(dest_buffers[dest_buffer_start + 3]);
                    dest_buffer_start += dest_buffer_len;
                    dst_start += dest->bpp;
                }
                dst_start += stride_offset;
            }
        }
        else
        {
            for (bix = 0; bix < to_pixel_count; bix++){
                for (bufferSet = 0; bufferSet < row_count; bufferSet++){
                    *(dst_start + 3) = 0xFF;
                    dst_start += dest->bpp;
                }
                dst_start += stride_offset;
            }
        }
    }
}


static inline int ScaleXAndPivot(const BitmapBgraPtr pSrc,
    const BitmapBgraPtr pDst, const InterpolationDetailsPtr details, float *lut)
{
    unsigned int line_ndx;
    LineContribType * contrib;

    contrib = ContributionsCalc(pDst->h, pSrc->w,
                        (double)pDst->h / (double)pSrc->w, details);
    if (contrib == NULL) {
        return 0;
    }

    int buffer = 4; //using buffer=5 seems about 6% better than most other non-zero values. 

    unsigned int source_buffer_len = pSrc->w * pSrc->bpp;
    float *sourceBuffers = (float *)malloc(sizeof(float) * source_buffer_len * buffer);

    unsigned int dest_buffer_len = pDst->h * pDst->bpp;
    float *destBuffers = (float *)malloc(sizeof(float) * dest_buffer_len * buffer);


    /* Scale each line */
    for (line_ndx = 0; line_ndx < pSrc->h; line_ndx += buffer) {

        ScaleXAndPivotRows(pSrc, line_ndx, MIN(pSrc->h - line_ndx, buffer), contrib->ContribRow, pDst,
            sourceBuffers, source_buffer_len, destBuffers, dest_buffer_len, lut);
    }

    free(sourceBuffers);
    free(destBuffers);
    
    ContributionsFree(contrib);

    return 1;
}




static inline void HalveRowByDivisor(const unsigned char* from, unsigned short * to, const unsigned int to_count, const int divisor, const int from_step=4, const int to_step=4){
    int to_b, from_b;
    const int to_bytes = to_count * to_step;
    const int divisor_stride = from_step * divisor;

    if (divisor == 2)
    {
        if (to_count % 2 == 0){
            for (to_b = 0, from_b = 0; to_b < to_bytes; to_b += 2*to_step, from_b += 4*from_step){
                for (int i = 0; i < 2*to_step; i++){
                    to[to_b + i] += from[from_b + i] + from[from_b + i + from_step];
                }
            }
        }
        else{
            for (to_b = 0, from_b = 0; to_b < to_bytes; to_b += to_step, from_b += 2 * from_step){
                for (int i = 0; i < to_step; i++){
                    to[to_b + i] += from[from_b + i] + from[from_b + i + from_step];
                }
            }
        }

    }
    else if (divisor == 3){
        for (to_b = 0, from_b = 0; to_b < to_bytes; to_b += to_step, from_b += 3 * from_step){
            for (int i = 0; i < to_step; i++){
                to[to_b + i] += from[from_b + i] + from[from_b + i + from_step] + from[from_b + i + 2*from_step];
            }
        }
    }
    else if (divisor == 4){
        for (to_b = 0, from_b = 0; to_b < to_bytes; to_b += to_step, from_b += 4 * from_step){
            for (int i = 0; i < to_step; i++){
                to[to_b + i] += from[from_b + i] + from[from_b + i + from_step] + from[from_b + i + 2*from_step] + from[from_b + i + 3*from_step];
            }
        }
    }
    else{
        for (to_b = 0, from_b = 0; to_b < to_bytes; to_b += to_step, from_b += divisor_stride){
            for (int i = 0; i < to_step; i++){
                for (int f = 0; f < divisor_stride; f += from_step){
                   to[to_b + i] += from[from_b + i + f];
                    
                }
            }
        }
    }
}

static int intlog2(unsigned int val) {
     int ret = -1;
    while (val != 0) {
        val >>= 1;
        ret++;
    }
    return ret;
}

static inline int isPowerOfTwo(unsigned int x)
{
    return ((x != 0) && !(x & (x - 1)));
}

static inline int HalveInternal(const BitmapBgraPtr from,
    const BitmapBgraPtr to, const int to_w, const int to_h, const int to_stride, const int divisor)
{

    int to_w_bytes = to_w * to->bpp;
    unsigned short *buffer = (unsigned short *)calloc(to_w_bytes, sizeof(unsigned short));

    int y, b, d;
    const unsigned short divisorSqr = divisor * divisor;
    unsigned int shift = 0;
    if (isPowerOfTwo(divisorSqr)){
        shift = intlog2(divisorSqr);
    }

    //TODO: Ensure that from is equal or greater than divisorx to_w and t_h

    for (y = 0; y < to_h; y++){
        memset(buffer, 0, sizeof(short) * to_w_bytes);
        for (d = 0; d < divisor; d++){
            HalveRowByDivisor(from->pixels + (y * divisor + d) * from->stride, buffer, to_w, divisor, from->bpp, to->bpp);
        }
        register unsigned char * dest_line = to->pixels + y * to_stride;

        if (shift == 2){
            for (b = 0; b < to_w_bytes; b++){
                dest_line[b] = buffer[b] >> 2;
            }
        }
        else if (shift == 3){
            for (b = 0; b < to_w_bytes; b++){
                dest_line[b] = buffer[b] >> 3;
            }
        }
        else if (shift > 0){
            for (b = 0; b < to_w_bytes; b++){
                dest_line[b] = buffer[b] >> shift;
            }
        }
        else{
            for (b = 0; b < to_w_bytes; b++){
                dest_line[b] = buffer[b] / divisorSqr;
            }
        }
    }

    free(buffer);

    return 1;
}

static inline int Halve(const BitmapBgraPtr from, const BitmapBgraPtr to, int divisor){
    return HalveInternal(from, to, to->w, to->h, to->stride,divisor);
}


static inline int HalveInPlace(const BitmapBgraPtr from, int divisor)
{
    int to_w = from->w / divisor;
    int to_h = from->h / divisor;
    int to_stride = to_w * from->bpp;
    int r = HalveInternal(from, from, to_w, to_h, to_stride, divisor);
    from->w = to_w;
    from->h = to_h;
    from->stride = to_stride;
    return r;
}




static void unpack24bitRow(int width, unsigned char* sourceLine, unsigned char* destArray){
	for (register unsigned int i = 0; i < width; i++){
        
        memcpy(destArray + i*4, sourceLine + i*3, 3);
        destArray[i*4 + 3] = 255;
	}
}





static void BgraSharpenInPlaceX(BitmapBgraPtr im, int pct)
{
    int x, y, current, prev, next, i;
    
    const int sx = im->w;
    const int sy = im->h;
    const int stride = im->stride;
    const int bpp = im->bpp;
    const float outer_coeff = -pct / 400.0;
    const float inner_coeff = 1 - 2 * outer_coeff;

    if (pct <= 0 || im->w < 3 || bpp < 3) return;
    
    for (y = 0; y < sy; y++)
    {
        unsigned char *row = im->pixels + y * stride;
        for (current = bpp, prev = 0, next = bpp + bpp; next < stride; prev = current, current = next, next += bpp){
            //We never sharpen the alpha channel
            for (int i = 0; i < 3; i++)
                row[current + i] = uchar_clamp_ff(outer_coeff * (float)row[prev + i] + inner_coeff * (float)row[current + i] + outer_coeff * (float)row[next + i]);
        } 
    }
}




#pragma managed


using namespace System;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;
using namespace ImageResizer::Resizing;
using namespace System::Diagnostics;
using namespace System::Collections::Specialized;
using namespace System::Runtime::InteropServices;

namespace ImageResizer{
	namespace Plugins{
		namespace FastScaling {

			public ref class BgraScaler
			{
			public:
                void ScaleBitmap(Bitmap^ source, Bitmap^ dest, Rectangle crop, Rectangle target, const InterpolationDetailsPtr details, IProfiler^ p){
					BitmapBgraPtr bbSource;
                    BitmapBgraPtr bbResult;
					try{
                        p->Start("SysDrawingToBgra",false);
                        bbSource = SysDrawingToBgra(source, crop);
                        bbResult = SysDrawingToBgra(dest, target);
                        p->Stop("SysDrawingToBgra", true, false);
                        
                        if (details->use_halving)
                            ScaleBgraWithHalving(bbSource, target.Width, target.Height, bbResult, details, p);
                        else
                            ScaleBgra(bbSource, target.Width, target.Height, bbResult, details, p);
                        
                        p->Start("BgraDispose", false);
					}finally{
                        if (bbSource != 0) {
                            DestroyBitmapBgra(bbSource);
                            if (bbResult == bbSource)
                                bbResult = 0;
                            bbSource = 0;
						}
                        if (bbResult != 0){
                            DestroyBitmapBgra(bbResult);
                            bbResult = 0;
						}
                        p->Stop("BgraDispose", true, false);

					}
				}
				 

			private:
                BitmapBgraPtr ScaleBgraWithHalving(BitmapBgraPtr source, int width, int height, BitmapBgraPtr dst, const InterpolationDetailsPtr details, IProfiler^ p){
                    p->Start("ScaleBgraWithHalving", false);
                    try{

                        int divisor = MIN(source->w / width, source->h / height);
                        BitmapBgraPtr tmp_im = 0;
                        
                        if (divisor > 1){
                            p->Start("Halving", false);

                            if (details->allow_source_mutation)
                              HalveInPlace(source, divisor);
                            else
                            {
                                tmp_im = CreateBitmapBgraPtr(source->w / divisor, source->h / divisor, false, 1, source->bpp);
                                if (!tmp_im)
                                    return 0;

                                Halve(source, tmp_im, divisor);

                                p->Stop("Halving", true, false);
                                return ScaleBgra(tmp_im, width, height, dst,details, p);
                            }
                            p->Stop("Halving", true, false);
                        }

                        return ScaleBgra(source, width, height, dst, details, p);
                    }
                    finally{
                        p->Stop("ScaleBgraWithHalving", true, false);
                    }
                    
                }

                void CopyBgra(BitmapBgraPtr src, BitmapBgraPtr dst)
                {
                    // TODO: check sizes / overflows

                    if (src->bpp == 4)
                    {
                        // recalculate line width as it can be different from the stride
                        for (int y = 0; y < src->h; y++)
                            memcpy(dst->pixels + y*dst->stride, src->pixels + y*src->stride, src->w*src->bpp);
                    }
                    else
                    {
                        for (int y = 0; y < src->h; y++)
                            unpack24bitRow(src->w, src->pixels + y*src->stride, dst->pixels + y*dst->stride);
                    }
                }

                BitmapBgraPtr ScaleBgra(BitmapBgraPtr source, int width, int height, BitmapBgraPtr dst, const InterpolationDetailsPtr details, IProfiler^ p){

                    p->Start("create image(dx x dy)", false);
                    if (!dst) dst = CreateBitmapBgraPtr(width, height, false, 1, source->bpp);
                    p->Stop("create image(dx x dy)", true, false);
                    if (dst == NULL) return NULL;


                    if (source->w == width && source->h == height){
                        // In case of both halfinplace and noresize we still need to copy the data
                        CopyBgra(source, dst);
                        return dst;
                    }


                    p->Start("ScaleBgra", true);
                    BitmapBgraPtr tmp_im = NULL;
                    float lut[256];
                    for (int n = 0; n < 256; n++) lut[n] = (float)n;

                    p->Start("create temp image(sy x dx)", false);
                    /* Scale horizontally  */
                    tmp_im = CreateBitmapBgraPtr(source->h, width, false, 1, source->bpp);
                   
                    
                    try{
                        if (tmp_im == NULL) {
                            return NULL;
                        }
                        p->Stop("create temp image(sy x dx)", true, false);

                        p->Start("scale and pivot to temp", false);
                        ScaleXAndPivot(source, tmp_im,details, lut);
                        p->Stop("scale and pivot to temp", true, false);

                        if (details->post_resize_sharpen_percent > 0){
                            p->Start("sharpening along X axis", false); 
                            BgraSharpenInPlaceX(tmp_im, details->post_resize_sharpen_percent);
                            p->Stop("sharpening along X axis", true, false);
                        }
                        
                        
                        p->Start("scale and pivot to final", false);
                        ScaleXAndPivot(tmp_im, dst, details, lut);
                        p->Stop("scale and pivot to final", true, false);

                        if (details->post_resize_sharpen_percent > 0){
                            p->Start("sharpening along Y axis", false);
                            BgraSharpenInPlaceX(dst, details->post_resize_sharpen_percent);
                            p->Stop("sharpening along Y axis", true, false);
                        }

                    }
                    finally{
                        p->Start("destroy temp image", false);
                        DestroyBitmapBgra(tmp_im);
                        p->Stop("destroy temp image", true, false);
                        p->Stop("ScaleBgra", true, false);
                    }
                    return dst;
				}

                void BgraToSysDrawing(BitmapBgraPtr source, Bitmap^ target, Rectangle targetArea){
					if (target->PixelFormat != PixelFormat::Format32bppArgb){
						throw gcnew ArgumentOutOfRangeException("target", "Invalid pixel format " + target->PixelFormat.ToString());
					}
                    BitmapData ^targetData;
					try{
						targetData = target->LockBits(targetArea, ImageLockMode::ReadOnly, target->PixelFormat);
						int sy = source->h;
						int sx = source->w;
						int i;
                        IntPtr^ scan0intptr = targetData->Scan0;
						void *scan0 = scan0intptr->ToPointer();
						for (i = 0; (i < sy); i++) {
                            void * linePtr = (void *)((unsigned long  long)scan0 + (targetData->Stride * (i + targetArea.Top)) + (targetArea.Left * 4));
							memcpy(linePtr, &source->pixels[i * source->stride], sx * 4);
						}
					}
					finally{
                        target->UnlockBits(targetData);
					}
				}

                BitmapBgraPtr SysDrawingToBgra(Bitmap^ source, Rectangle from){
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

					BitmapBgraPtr im = CreateBitmapBgraPtr(sx, sy, false, false);
					
					BitmapData ^sourceData;
					try{
						sourceData = source->LockBits(from, ImageLockMode::ReadWrite, source->PixelFormat);

						IntPtr^ scan0intptr = sourceData->Scan0;

						void *scan0 = scan0intptr->ToPointer();
						void *linePtr = (void *)((unsigned long long)scan0 + (unsigned long  long)((sourceData->Stride * from.Top) + (from.Left * (hasAlpha ? 4 : 3))));
						
                        im->pixels = (unsigned char *)linePtr;
                        im->pixelInts = (unsigned int *)linePtr;
                        im->stride = sourceData->Stride;
					}
					finally{
						source->UnlockBits(sourceData);
					}
					im->w = sx;
					im->h = sy;

                    im->hasAlpha = hasAlpha;
                    if (!hasAlpha)
                        im->bpp = 3;
					return im;
				}
			};


			public ref class FastScalingPlugin : public ImageResizer::Resizing::BuilderExtension, IPlugin
			{
			protected:
				virtual RequestedAction InternalGraphicsDrawImage(ImageState^ s, Bitmap^ dest, Bitmap^ source, array<PointF>^ targetArea, RectangleF sourceArea, ImageAttributes^ imageAttributes) override{
                    
                    NameValueCollection ^query = s->settingsAsCollection();

                    String^ fastScale = query->Get("fastscale");
					String^ sTrue = "true";

                    
					if (fastScale != sTrue){
						return RequestedAction::None;
					}
                    
                    int withHalving = 0;
                    String^ turbo = query->Get("turbo");
                    if (turbo == sTrue)
                        withHalving = 1;

                    double blur = System::String::IsNullOrEmpty(query->Get("blur")) ? 1.0 :
                        System::Double::Parse(query->Get("blur"));
                    
                    double window = System::String::IsNullOrEmpty(query->Get("window")) ? 0 :
                        System::Double::Parse(query->Get("window"));

                    double sharpen = System::String::IsNullOrEmpty(query->Get("sharpen")) ? 0 :
                        System::Double::Parse(query->Get("sharpen"));


					RectangleF targetBox = ImageResizer::Util::PolygonMath::GetBoundingBox(targetArea);
					if (targetBox.Location != targetArea[0] || targetBox.Width != (targetArea[1].X - targetArea[0].X)){
						return RequestedAction::None;
                    }
                    
                    InterpolationDetailsPtr details;
                    details = DetailsDefault();
                    
                    if (query->Get("f") == "1"){
                        details = DetailsGeneralCubic();
                    }
                    if (query->Get("f") == "2"){
                        details = DetailsCatmullRom();
                    }
                    if (query->Get("f") == "3"){
                        details = DetailsMitchell();
                    }
                    if (query->Get("f") == "4"){
                        details = DetailsRobidoux();
                    }
                    if (query->Get("f") == "5"){
                        details = DetailsRobidouxSharp();
                    }
                    if (query->Get("f") == "6"){
                        details = DetailsHermite();
                    }
                    if (query->Get("f") == "7"){
                        details = DetailsLanczos();
                    }
                    details->allow_source_mutation = true;
                    details->use_halving = withHalving;
                    details->blur *= blur;
                    details->post_resize_sharpen_percent = (int)sharpen;
                    if (window != 0) details->window = window;
                        
                    BgraScaler ^scaler = gcnew BgraScaler();
                    scaler->ScaleBitmap(source, dest, Util::PolygonMath::ToRectangle(sourceArea), Util::PolygonMath::ToRectangle(targetBox), details, s->Job->Profiler);
                    free(details);
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