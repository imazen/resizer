#pragma once
/**

  Currently we only support BGR24 and BGRA32 pixel formats.
  (And BGR32, where we ignore the alpha channel, but that's not precisely a separate format)
  Eventually we will need to support
  * 8-bit grayscale
  * CMYK
  * YCbCr
  and possibly others. For V1, the API we expose is only used by projects in the same repository, running under the same tests.
  In V2, we can change the API as we wish; we are not constrained to what we design here.
  Perhaps it is best to explicitly limit the structure to represent what we process at this time?
 If our buffers and structures actually describe their contents, then we need to support all permitted values in all functions. This is problematic.
* We heavily experimented with LUV and XYZ color spaces, but determined that better results occur using RGB linear.
* A custom sigmoidized color space could perhaps improve things, but would introduce significant overhead.
**/

/* Proposed changes

*/

#include "status_code.h"
#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>


#ifdef __cplusplus
extern "C" {
#endif

struct _Context;


typedef void * (*context_calloc_function)(struct _Context * context, size_t count, size_t element_size, const char * file, int line);
typedef void * (*context_malloc_function)(struct _Context * context, size_t byte_count, const char * file, int line);
typedef void   (*context_free_function)  (struct _Context * context, void * pointer, const char * file, int line);
typedef void   (*context_terminate_function)  (struct _Context * context);

typedef struct _HeapManager{
    context_calloc_function _calloc;
    context_malloc_function _malloc;
    context_free_function  _free;
    context_terminate_function _context_terminate;
    void * _private_state;
} HeapManager;

typedef struct _ErrorCallstackLine{
    const char * file;
    int line;
} ErrorCallstackLine;

typedef struct _ErrorInfo{
  StatusCode reason;
  ErrorCallstackLine callstack[8]; 
  int callstack_count;
  int callstack_capacity;
} ErrorInfo;


typedef enum _ProfilingEntryFlags {
    Profiling_start = 2,
    Profiling_start_allow_recursion = 2 | 4,
    Profiling_stop = 8,
    Profiling_stop_assert_started = 8 | 16,
    Profiling_stop_children = 8 | 16 | 32,

} ProfilingEntryFlags;


typedef struct{
    int64_t time;
    const char * name;
    ProfilingEntryFlags flags;
} ProfilingEntry;

typedef struct{
    ProfilingEntry * log;
    uint32_t count;
    uint32_t capacity;
} ProfilingLog;

typedef struct _Context {
    ErrorInfo error;
    HeapManager heap;
    ProfilingLog log;
} Context;

void DefaultHeapManager_initialize(HeapManager * context);

void Context_initialize(Context * context);
void Context_terminate(Context * context);

Context * Context_create(void);
void Context_destroy(Context * context);

void Context_set_last_error(Context * context, StatusCode code, const char * file, int line);
void Context_add_to_callstack(Context * context, const char * file, int line);


const char * Context_error_message(Context * context, char * buffer, size_t buffer_size);
bool Context_has_error(Context * context);
int  Context_error_reason(Context * context);

void * Context_calloc(Context * context, size_t, size_t, const char * file, int line);
void * Context_malloc(Context * context, size_t, const char * file, int line);
void Context_free(Context * context, void * pointer, const char * file, int line);

bool Context_enable_profiling(Context * context,uint32_t default_capacity);

int64_t Context_get_profiler_ticks_per_second(Context * context);
ProfilingLog * Context_get_profiler_log(Context * context);


#define CONTEXT_SET_LAST_ERROR(context, status_code) Context_set_last_error(context, status_code, __FILE__, __LINE__)
#define CONTEXT_calloc(context, instance_count, element_size) Context_calloc(context, instance_count, element_size, __FILE__, __LINE__)
#define CONTEXT_calloc_array(context, instance_count, type_name) (type_name *) Context_calloc(context, instance_count, sizeof(type_name), __FILE__, __LINE__)
#define CONTEXT_malloc(context, byte_count) Context_malloc(context, byte_count, __FILE__, __LINE__)
#define CONTEXT_free(context, pointer) Context_free(context, pointer, __FILE__, __LINE__)
#define CONTEXT_error(context, status_code) CONTEXT_SET_LAST_ERROR(context,status_code)

#define CONTEXT_add_to_callstack(context) Context_add_to_callstack(context, __FILE__,__LINE__)

#define CONTEXT_error_return(context) Context_add_to_callstack(context, __FILE__,__LINE__); return false


//Compact format for bitmaps. sRGB or gamma adjusted - *NOT* linear
typedef enum _BitmapPixelFormat {
    Bgr24 = 3,
    Bgra32 = 4,
    Gray8 = 1
} BitmapPixelFormat;


typedef enum _BitmapCompositingMode {
    Replace_self = 0,
    Blend_with_self = 1,
    Blend_with_matte = 2
} BitmapCompositingMode;

//non-indexed bitmap
typedef struct BitmapBgraStruct {

    //bitmap width in pixels
    uint32_t w;
    //bitmap height in pixels
    uint32_t h;
    //byte length of each row (may include any amount of padding)
    uint32_t stride;
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

    //If true, we can reuse the allocated memory for other purposes.
    bool can_reuse_space;

    BitmapPixelFormat fmt;

    //When using compositing mode blend_with_matte, this color will be used. We should probably define this as always being sRGBA, 4 bytes.
    uint8_t matte_color[4];

    BitmapCompositingMode compositing_mode;

} BitmapBgra;




typedef struct RendererStruct Renderer;

typedef enum _InterpolationFilter {
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
    Filter_CubicBSpline,
    Filter_Lanczos3Windowed,
    Filter_Lanczos3SharpWindowed,
    Filter_Lanczos2Windowed,
    Filter_Lanczos2SharpWindowed,
    Filter_CatmullRomFast,
    Filter_CatmullRomFastSharp
} InterpolationFilter;

struct InterpolationDetailsStruct;
typedef double (*detailed_interpolation_method)(const struct InterpolationDetailsStruct *, double);


typedef struct InterpolationDetailsStruct {
    //1 is the default; near-zero overlapping between windows. 2 overlaps 50% on each side.
    double window;
    //Coefficients for bucubic weighting
    double p1, p2, p3, q1, q2, q3, q4;
    //Blurring factor when > 1, sharpening factor when < 1. Applied to weights.
    double blur;

    //pointer to the weight calculation function
    detailed_interpolation_method filter;
    //How much sharpening we are requesting
    float sharpen_percent_goal;

} InterpolationDetails;


typedef struct ConvolutionKernelStruct{
    float * kernel;
    uint32_t width;
    uint32_t radius;
    float threshold_min_change; //These change values are on a somewhat arbitrary scale between 0 and 4;
    float threshold_max_change;
    float * buffer;
} ConvolutionKernel;

typedef struct RenderDetailsStruct{
    //Interpolation and scaling details
    InterpolationDetails * interpolation;
    //How large the interoplation window needs to be before we even attempt to apply a sharpening
    //percentage to the given filter
    float minimum_sample_window_to_interposharpen;


    // If possible to do correctly, halve the image until it is [interpolate_last_percent] times larger than needed. 3 or greater reccomended. Specify -1 to disable halving.
    float interpolate_last_percent;

    //If true, only halve when both dimensions are multiples of the halving factor
    bool halve_only_when_common_factor;

    //The actual halving factor to use.
    uint32_t halving_divisor;


    ConvolutionKernel * kernel_a;
    ConvolutionKernel * kernel_b;


    //If greater than 0, a percentage to sharpen the result along each axis;
    float sharpen_percent_goal;

    //If true, we should apply the color matrix
    bool apply_color_matrix;

    float color_matrix_data[25];
    float *color_matrix[5];

    //Transpose, flipx, flipy - combined, these give you all 90 interval rotations
    bool post_transpose;
    bool post_flip_x;
    bool post_flip_y;

    //Enables profiling
    bool enable_profiling;

} RenderDetails;

typedef struct LookupTablesStruct *LookupTablesPtr;

typedef struct LookupTablesStruct {
    float srgb_to_linear[256]; //Converts 0..255 -> 0..1, but knowing that 0.255 has sRGB gamma.
    float linear[256]; //Converts 0..255 -> 0..1, linear mapping
} LookupTables;



BitmapBgra * BitmapBgra_create(Context * context, int sx, int sy, bool zeroed, BitmapPixelFormat format);
BitmapBgra * BitmapBgra_create_header(Context * context, int sx, int sy);

RenderDetails * RenderDetails_create(Context * context);

Renderer * Renderer_create(Context * context, BitmapBgra * source, BitmapBgra * canvas, RenderDetails * details);
Renderer * Renderer_create_in_place(Context * context, BitmapBgra * editInPlace, RenderDetails * details);
bool Renderer_perform_render(Context * context, Renderer * r);
void Renderer_destroy(Context * context, Renderer * r);
void BitmapBgra_destroy(Context * context, BitmapBgra * im);

//These filters are stored in a struct as function pointers, which I assume means they can't be inlined. Likely 5 * w * h invocations.
double filter_flex_cubic(const InterpolationDetails * d, double x);
double filter_bicubic_fast(const InterpolationDetails * d, double t);
double filter_sinc_2(const InterpolationDetails * d, double t);
double filter_box(const InterpolationDetails * d, double t);
double filter_triangle(const InterpolationDetails * d, double t);
double filter_sinc_windowed(const InterpolationDetails * d, double t);

double InterpolationDetails_percent_negative_weight(const InterpolationDetails * details);


InterpolationDetails * InterpolationDetails_create(Context * context);
InterpolationDetails * InterpolationDetails_create_bicubic_custom(Context * context,double window, double blur, double B, double C);
InterpolationDetails * InterpolationDetails_create_custom(Context * context,double window, double blur, detailed_interpolation_method filter);
InterpolationDetails * InterpolationDetails_create_from(Context * context,InterpolationFilter filter);
void InterpolationDetails_destroy(Context * context, InterpolationDetails *);


uint32_t BitmapPixelFormat_bytes_per_pixel (BitmapPixelFormat format);

typedef struct {
    float *Weights;/* Normalized weights of neighboring pixels */
    int Left;      /* Bounds of source pixels window */
    int Right;
} PixelContributions;/* Contirbution information for a single pixel */

typedef struct {
    PixelContributions *ContribRow; /* Row (or column) of contribution weights */
    uint32_t WindowSize;      /* Filter window size (of affecting source pixels) */
    uint32_t LineLength;      /* Length of line (no. or rows / cols) */
    double percent_negative; /* Estimates the sharpening effect actually applied*/
} LineContributions;

LineContributions * LineContributions_create(Context * context, const uint32_t output_line_size, const uint32_t input_line_size, const InterpolationDetails * details);
void LineContributions_destroy(Context * context, LineContributions * p);

// do these need to be public??
void free_lookup_tables(void);
LookupTables * get_lookup_tables(void);


ConvolutionKernel * ConvolutionKernel_create(Context * context, uint32_t radius);
void ConvolutionKernel_destroy(Context * context, ConvolutionKernel * kernel);


ConvolutionKernel* ConvolutionKernel_create_guassian(Context * context, double stdDev, uint32_t radius);
//The only error these 2 could generate would be a null pointer. Should they have a context just for this?
double ConvolutionKernel_sum(ConvolutionKernel* kernel);
void ConvolutionKernel_normalize(ConvolutionKernel* kernel, float desiredSum);
ConvolutionKernel* ConvolutionKernel_create_guassian_normalized(Context * context, double stdDev, uint32_t radius);
ConvolutionKernel* ConvolutionKernel_create_guassian_sharpen(Context * context, double stdDev, uint32_t radius);

#ifdef __cplusplus
}
#endif
