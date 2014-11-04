#pragma once
#pragma unmanaged


typedef struct{
    int w;
    int h;
    int stride;
    unsigned char *pixels;
    unsigned int *pixelInts; //Type aliasing
    bool hasAlpha; 
    bool ownMem; 
    int bpp;
} BitmapBgra;

typedef BitmapBgra *BitmapBgraPtr;

typedef struct InterpolationDetailsStruct *InterpolationDetailsPtr;

typedef double(*detailed_interpolation_method)(InterpolationDetailsPtr, double);

typedef struct InterpolationDetailsStruct{
    double window;
    double p1, p2, p3, q1, q2, q3, q4;
    double blur;
    detailed_interpolation_method filter;
    bool use_halving;
    bool allow_source_mutation;
    int post_resize_sharpen_percent;
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


static BitmapBgraPtr CreateBitmapBgraPtr(int sx, int sy, int zeroed, bool alloc = true, int bpp = 4)
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




static void unpack24bitRow(int width, unsigned char* sourceLine, unsigned char* destArray){
    for (register unsigned int i = 0; i < width; i++){

        memcpy(destArray + i * 4, sourceLine + i * 3, 3);
        destArray[i * 4 + 3] = 255;
    }
}


