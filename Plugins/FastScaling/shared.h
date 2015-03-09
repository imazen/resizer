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
#include "bitmap_formats.h"
#include "color_spaces.h"
#include "convolution.h"

enum InterpolationFilter{
    Filter_CubicFast,
    Filter_Cubic,
    Filter_CatmullRom,
    Filter_Mitchell,
    Filter_Robidoux,
    Filter_RobidouxSharp,
    Filter_Hermite,
    Filter_Lanczos3,
    Filter_Lanczos3Sharp,
    Filter_Lanczos2,
    Filter_Lanczos2Sharp,
    Filter_Triangle,
    Filter_Linear,
    Filter_Box,
};

enum Rotate{
    RotateNone = 0,
    Rotate90 = 1,
    Rotate180 = 2,
    Rotate270 = 3
};


typedef struct InterpolationDetailsStruct *InterpolationDetailsPtr;

typedef double(*detailed_interpolation_method)(InterpolationDetailsPtr, double);

typedef struct InterpolationDetailsStruct{
    //1 is the default; near-zero overlapping between windows. 2 overlaps 50% on each side.
    double window;
    //Coefficients for bucubic weighting
    double p1, p2, p3, q1, q2, q3, q4;
    //Blurring factor when > 1, sharpening factor when < 1. Applied to weights.
    double blur;

    //pointer to the weight calculation function
    detailed_interpolation_method filter;

    float sharpen_percent_goal;
    bool sharpen_successful;

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


typedef struct LookupTablesStruct *LookupTablesPtr;

typedef struct LookupTablesStruct{
    const float srgb_to_linear[256]; //Converts 0..255 -> 0..1, but knowing that 0.255 has sRGB gamma.
    const float linear[256]; //Converts 0..255 -> 0..1
    const uint8_t linear_to_srgb[1024]; //Converts from 0..1023 to 0.255, going from linear to sRGB gamma.
} LookupTables;




typedef struct RenderDetailsStruct *RenderDetailsPtr;


typedef struct RenderDetailsStruct{

    //Original scaling values, if required.
    //scale/scale_h are sans-transpose. 
    //final_w/final_h is the actual result size expected afer all operations
    //uint32_t from_w, scale_w, final_w, from_h, final_h, scale_h;


    InterpolationDetailsPtr interpolation;

    float minimum_sample_window_to_interposharpen;

    
    bool apply_color_matrix;

    // If possible to do correctly, halve the image until it is [interpolate_last_percent] times larger than needed. 3 or greater reccomended. Specify -1 to disable halving.
    float interpolate_last_percent; 

    //If true, only halve when both dimensions are multiples of the halving factor
    bool halve_only_when_common_factor;

    //The actual halving factor to use.
    uint32_t halving_divisor;



    float * kernel_a;
    float kernel_a_min;
    float kernel_a_max;
    uint32_t kernel_a_radius;

    float * kernel_b;
    float kernel_b_min;
    float kernel_b_max;
    uint32_t kernel_b_radius;



    //If greater than 0, a percentage to sharpen the result along each axis;
    float sharpen_percent_goal;

    
    float color_matrix_data[25];
    float *color_matrix[5];

    bool post_transpose;
    bool post_flip_x;
    bool post_flip_y;
 
}RenderDetails;

static InterpolationDetailsPtr CreateInterpolationDetails(){
    InterpolationDetailsPtr d = (InterpolationDetailsPtr)calloc(1, sizeof(InterpolationDetails));
    d->blur = 1;
    d->window = 2;
    d->p1 = d->q1 = 0;
    d->p2 = d->q2 = d->p3 = d->q3 = d->q4 = 1;
    d->sharpen_percent_goal = 0;
    d->sharpen_successful = false;
    return d;
}

static RenderDetailsPtr CreateRenderDetails(){
    RenderDetailsPtr d = (RenderDetailsPtr)calloc(1, sizeof(RenderDetails));
    for (int i = 0; i < 5; i++){
        d->color_matrix[i] = &(d->color_matrix_data[i * 5]);
    }
    d->interpolate_last_percent = 3;
    d->halve_only_when_common_factor = true;
    d->minimum_sample_window_to_interposharpen = 1.5;
    d->apply_color_matrix = false;
    return d;
}


static void DestroyRenderDetails(RenderDetailsPtr d){
    if (d->interpolation != NULL) free(d->interpolation);

    if (d->kernel_a != NULL) ir_free(d->kernel_a);

    if (d->kernel_b != NULL) ir_free(d->kernel_b);

    free(d);
}




static LookupTablesPtr GetLookupTables(){
    static LookupTablesPtr table = NULL;

    if (table == NULL){
        LookupTablesPtr temp = (LookupTablesPtr)ir_malloc(sizeof(LookupTablesStruct));

        // Gamma correction
        // http://www.4p8.com/eric.brasseur/gamma.html#formulas

        // Store gamma adjusted in 256-511, linear in 0-255

        float *lin = (float *)temp->linear;
        float *to_lin = (float *)temp->srgb_to_linear;
        uint8_t *to_srgb = (uint8_t *)temp->linear_to_srgb;
        
        const float a = 0.055f;

        for (uint32_t n = 0; n < 256; n++)
        {
            const float s = ((float)n) / 255.0f;
            lin[n] = s;

            if (s <= 0.04045f)
                to_lin[n] = s / 12.92f;
            else
                to_lin[n] = pow((s + a) / (1 + a), 2.4f);
        }
        for (uint32_t n = 0; n < 1024; n++){
            to_srgb[n] = uchar_clamp_ff(linear_to_srgb((float)n / 1024.0f));
        }
        
        
        if (table == NULL){
            //A race condition could cause a 3KB, one-time memory leak between these two lines. 
            //we're OK with that.
            table = temp;
        }
        else{
            ir_free(temp);
        }
    }
    return table;
}
