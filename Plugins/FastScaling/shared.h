#pragma once
#pragma unmanaged

#include <limits.h>
#include <malloc.h>

#define ENABLE_INTERNAL_PREMULT
#define ENABLE_COMPOSITING // needs premult

#define ALIGN_ALLOCATIONS
#ifdef ALIGN_ALLOCATIONS
#define ir_malloc(size) _aligned_malloc(size, 32)
#define ir_free(ptr) _aligned_free(ptr)
_declspec(noalias) _declspec(restrict) inline void* _ir_aligned_calloc(size_t count, size_t elsize, size_t alignment){
    if (elsize == 0 || count >= SIZE_MAX / elsize) { return NULL; } // Watch out for overflow
    size_t size = count * elsize;
    void *memory = _aligned_malloc(size, alignment);
    if (memory != NULL) { memset(memory, 0, size); }
    return memory; 
}
#define ir_calloc(count, element_size) _ir_aligned_calloc(count,element_size, 32)
#else
#define ir_malloc(size) malloc(size)
#define ir_free(ptr) free(ptr)
#define ir_calloc(count, element_size) calloc(count,element_size)
#endif

#include "math_functions.h"
#include "color_spaces.h"
#include "bitmap_formats.h"
#include "bitmap_compositing.h"
#include "convolution.h"


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

    float integrated_sharpen_percent;
    float * convolution_kernel;
    int kernel_radius;
    float kernel_threshold;
    float unsharp_sigma;
    //If greater than 0, a percentage to sharpen the result along each axis;
    float post_resize_sharpen_percent;
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
    d->unsharp_sigma = 1.4f;
    d->linear_sharpen = true;
    d->kernel_threshold = 0;
    d->use_luv = true;
    return d;
}
