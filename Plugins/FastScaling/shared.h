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
    //Multiplier applied to negative weights (certain filters only). Creates sharpening effect when window is large enough. may need to be adjusted based on window size.
    double negative_multiplier;
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

    double integrated_sharpen_percent;
    float * convolution_kernel;
    int kernel_radius;
    float kernel_threshold;
    float unsharp_sigma;
    //If greater than 0, a percentage to sharpen the result along each axis;
    double post_resize_sharpen_percent;
    //Reserved for passing data to new filters
    int filter_var_a;
    bool linear_sharpen;
    bool use_luv;
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


#define IR_PI  double (3.1415926535897932384626433832795)
#define IR_SINC(value) (value == 0 ? 1 : sin(value * IR_PI) / (value * IR_PI))

#define IR_GUASSIAN(x, stdDev) (exp((-x * x) / (2 * stdDev * stdDev)) / (sqrt(2 * IR_PI) * stdDev))

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


static inline void linear_to_luv_2(float * bgr){
    //Observer= 2°, Illuminant= D65

    const float r = bgr[2];
    const float g = bgr[1];
    const float b = bgr[0];
 
    const float x = r * 0.4124 + g * 0.3576 + b * 0.1805;
    const float y = r * 0.2126 + g * 0.7152 + b * 0.0722;
    const float z = r * 0.0193 + g * 0.1192 + b * 0.9505;

    const float u = (4 * x) / (x + (15 * y) + (3 * z));
    const float v = (9 * y) / (x + (15 * y) + (3 * z));


    float var_Y = y / 100;
    if (var_Y > 0.008856) var_Y = pow(var_Y, (1 / 3));
    else                    var_Y = (7.787 * var_Y) + (16 / 116);

    const float ref_X = 95.047;
    const float ref_Y = 100.000;
    const float ref_Z = 108.883;

    const float ref_U = (4 * ref_X) / (ref_X + (15 * ref_Y) + (3 * ref_Z));
    const float ref_V = (9 * ref_Y) / (ref_X + (15 * ref_Y) + (3 * ref_Z));

    const float luv_l = (116 * var_Y) - 16;
    const float luv_u = 13 * luv_l * (u - ref_U);
    const float luv_v = 13 * luv_l * (v - ref_V);
    bgr[0] = luv_l;
    bgr[1] = luv_u;
    bgr[2] = luv_v;

}


static inline void linear_to_luv(float * bgr){
    //Observer= 2°, Illuminant= D65

    const float xn = 0.312713;
    const float yn = 0.329016;
    const float Yn = 1.0;
    const float un = 4 * xn / (-2 * xn + 12 * yn + 3);
    const float vn = 9 * yn / (-2 * xn + 12 * yn + 3);
    const float y_split = 0.00885645;
    const float y_adjust = 903.3;

    const float R = bgr[2];
    const float G = bgr[1];
    const float B = bgr[0];

    if (R == 0 && G == 0 && B == 0){
        bgr[0] = 0;
        bgr[1] = bgr[2] = 100;
        return;
    }

    const float X = 0.412453*R + 0.35758 *G + 0.180423*B;
    const float Y = 0.212671*R + 0.71516 *G + 0.072169*B;
    const float Z = 0.019334*R + 0.119193*G + 0.950227*B;



    const float Yd = Y / Yn;

    const float u = 4 * X / (X + 15 * Y + 3 * Z);
    const float v = 9 * Y / (X + 15 * Y + 3 * Z);
    const float L = bgr[0] /* L */ = Yd > y_split ? (116 * pow(Yd, (float)(1.0 / 3.0)) - 16) : y_adjust * Yd;
    bgr[1]/* U */ = 13 * L*(u - un) + 100;
    bgr[2] /* V */ = 13 * L*(v - vn) + 100;
}

static inline void luv_to_linear(float * luv){
    //D65 white point :
    const float L = luv[0];
    const float U = luv[1] - 100;
    const float V = luv[2] - 100;
    if (L == 0){
        luv[0] = luv[1] = luv[2] = 0;
        return;
    }

    const float xn = 0.312713;
    const float yn = 0.329016;
    const float Yn =1.0;
    const float un = 4 * xn / (-2 * xn + 12 * yn + 3);
    const float vn = 9 * yn / (-2 * xn + 12 * yn + 3);
    const float y_adjust_2 = 0.00110705645;

    const float u = U / (13 * L) + un;
    const float v = V / (13 * L) + vn;
    const float Y = L > 8 ? Yn * pow((L + 16) / 116, 3) : Yn * L * y_adjust_2;
    const float X = (9 / 4.0) * Y * u / v;// -9 * Y * u / ((u - 4) * v - u * v) = (9 / 4) * Y * u / v;
    const float Z = (9 * Y - 15 * v * Y - v * X) / (3 * v);


    const float r = 3.240479*X - 1.53715 *Y - 0.498535*Z;
    const float g = -0.969256*X + 1.875991*Y + 0.041556*Z;
    const float b = 0.055648*X - 0.204043*Y + 1.057311*Z;
    luv[0] = b; luv[1] = g; luv[2] = r;

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
    d->window = 2;
    d->p1 = d->q1 = 0;
    d->p2 = d->q2 = d->p3 = d->q3 = d->q4 = 1;
    d->allow_source_mutation = false;
    d->halve_only_when_common_factor = false;
    d->post_resize_sharpen_percent = 0;
    d->use_halving = false;
    d->negative_multiplier = 1;
    d->use_interpolation_for_percent = 0.3;
    d->integrated_sharpen_percent = 0;
    d->kernel_radius = 0;
    d->unsharp_sigma = 1.4;
    d->linear_sharpen = true;
    d->kernel_threshold = 0;
    d->use_luv = true;
    return d;
}
