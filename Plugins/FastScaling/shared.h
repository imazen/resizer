#pragma once
#pragma unmanaged

#include <limits.h>

#define ENABLE_INTERNAL_PREMULT
#define ENABLE_COMPOSITING // needs premult


enum BitmapPixelFormat {
    None = 0,
    Bgr24 = 24,
    Bgra32 = 32,
    Gray8 = 8
};
enum BitmapCompositingMode{
    Replace_self = 0,
    Blend_with_self = 1,
    Blend_with_matte = 2
};
typedef struct BitmapBgraStruct *BitmapBgraPtr;

//non-indexed bitmap
typedef struct BitmapBgraStruct{

    //bitmap width in pixels
    int32_t w;
    //bitmap height in pixels
    int32_t h;
    //byte length of each row (may include any amount of padding)
    int32_t stride;
    //pointer to pixel 0,0; should be of length > h * stride
    unsigned char *pixels;
    //If true, we don't dispose of *pixels when we dispose the struct
    bool borrowed_pixels;
    //If false, we can even ignore the alpha channel on 4bpp
    bool alpha_meaningful;
    //If false, we can edit pixels without affecting the stride
    bool pixels_readonly;
    //If false, we can change the stride of the image.
    bool stride_readonly;

    //TODO: rename to bytes_pp
    int32_t bpp;

    BitmapPixelFormat pixel_format;

    //When using compositing mode blend_with_matte, this color will be used
    unsigned char *matte_color;
    ///If true, we don't dispose of *pixels when we dispose the struct
    bool borrowed_matte_color;

    BitmapCompositingMode compositing_mode;

} BitmapBgra;



typedef struct InterpolationDetailsStruct *InterpolationDetailsPtr;

typedef double(*detailed_interpolation_method)(InterpolationDetailsPtr, double);

typedef struct InterpolationDetailsStruct{
    //0.5 is the sane default; minimal overlapping between windows
    double window;
    //Coefficients for bucubic weighting
    double p1, p2, p3, q1, q2, q3, q4;
    //Blurring factor when > 1, sharpening factor when < 1. Applied to weights.
    double blur;
    //pointer to the weight calculation function
    detailed_interpolation_method filter;
    //If true, use area averaging for initial reduction
    bool use_halving;
    //If true, only halve when both dimensions are multiples of the halving factor
    bool halve_only_when_common_factor;
    //The final percentage (0..1) of scaling which must be perfomed by true interpolation
    double use_interpolation_for_percent;
    //If true, we can 'reuse' the source image as a performance optimization when halving
    bool allow_source_mutation;
    //If greater than 0, a percentage to sharpen the result along each axis;
    double post_resize_sharpen_percent;
    //Reserved for passing data to new filters
    int filter_var_a;

}InterpolationDetails;


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



#ifndef MIN
#   define MIN(a,b) ((a)<(b)?(a):(b))
#endif
#   define MIN3(a,b,c) ((a)<(b)?(MIN(a,c)):(MIN(b,c)))
#ifndef MAX
#   define MAX(a,b) ((a)<(b)?(b):(a))
#endif
#ifndef MAX3
#   define MAX3(a,b,c) ((a)<(b)?(MAX(b,c)):(MAX(a,c)))
#endif
#ifndef NULL
#   define NULL 0
#endif

#define CLAMP(x, low, high)  (((x) > (high)) ? (high) : (((x) < (low)) ? (low) : (x)))


static inline float
linear_to_srgb(float clr) {
    // Gamma correction
    // http://www.4p8.com/eric.brasseur/gamma.html#formulas

    if (clr <= 0.0031308)
        return 12.92f * clr * 255.0f;

    // a = 0.055; ret ((1+a) * s**(1/2.4) - a) * 255
    return 1.055f * pow(clr, 0.41666666f) * 255.0f - 14.025f;
}

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

static BitmapBgraPtr CreateBitmapBgraHeader(int sx, int sy){
    BitmapBgraPtr im;

    if (overflow2(sx, sy) || overflow2(sizeof(int *), sy) || overflow2(sizeof(int), sx)) {
        return NULL;
    }

    im = (BitmapBgra *)malloc(sizeof(BitmapBgra));
    if (!im) {
        return NULL;
    }
    memset(im, 0, sizeof(BitmapBgra));
    im->w = sx;
    im->h = sy;
    im->pixels = NULL;
    im->pixels_readonly = true;
    im->stride_readonly = true;
    im->borrowed_pixels = true;
    return im;
}


static BitmapBgraPtr CreateBitmapBgra(int sx, int sy, bool zeroed, int bpp)
{
    BitmapBgraPtr im = CreateBitmapBgraHeader(sx, sy);
    if (im == NULL){ return NULL; }

    im->bpp = bpp;
    im->stride = im->w * bpp;
    im->pixels_readonly = false;
    im->stride_readonly = false;
    im->borrowed_pixels = false;
    im->alpha_meaningful = bpp == 4;
    if (zeroed){
        im->pixels = (unsigned char *)calloc(sy * im->stride, sizeof(unsigned char));
    }
    else{
        im->pixels = (unsigned char *)malloc(sy * im->stride);
    }
    if (!im->pixels) {
        free(im);
        return 0;
    }
    return im;
}


static void DestroyBitmapBgra(BitmapBgraPtr im)
{
    int i;
    if (im == NULL) return;

    if (!im->borrowed_pixels) {
        free(im->pixels);
    }
    free(im);
}




static void unpack24bitRow(int width, unsigned char* sourceLine, unsigned char* destArray){
    for (register unsigned int i = 0; i < width; i++){

        memcpy(destArray + i * 4, sourceLine + i * 3, 3);
        destArray[i * 4 + 3] = 255;
    }
}


static InterpolationDetailsPtr CreateInterpolationDetails(){
    InterpolationDetailsPtr d = (InterpolationDetails *)calloc(1, sizeof(InterpolationDetails));
    d->blur = 1;
    d->window = 0.5;
    d->p1 = d->q1 = 0;
    d->p2 = d->q2 = d->p3 = d->q3 = d->q4 = 1;
    d->allow_source_mutation = false;
    d->halve_only_when_common_factor = false;
    d->post_resize_sharpen_percent = 0;
    d->use_halving = false;
    d->use_interpolation_for_percent = 0.3;
    return d;
}
