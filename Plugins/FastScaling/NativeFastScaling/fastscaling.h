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

combine BitmapBgraStruct->bpp and BitmapBgraStruct->pixel_format somehow.

Rename things for clarity.
Prefix things, perhaps?

*/

#include "status_code.h"
#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>


#ifdef __cplusplus
extern "C" {
#endif

typedef void * (*calloc_function)(size_t, size_t);
typedef void * (*malloc_function)(size_t);
typedef void (*free_function)(void *);

typedef struct _Context {
    struct {
	const char * file;
	int line;
	StatusCode reason;
    } error;
    malloc_function malloc;
    free_function free;
    calloc_function calloc;
} Context;

void Context_initialize(Context * context);
void Context_set_last_error(Context * context, StatusCode code, const char * file, int line);
const char * Context_last_error_message(Context * context, char * buffer, size_t buffer_size);
bool Context_has_error(Context * context);
int Context_error_reason(Context * context);

#define CONTEXT_SET_LAST_ERROR(context, status_code) Context_set_last_error(context, status_code, __FILE__, __LINE__)

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
    int32_t flags;
} ProfilingEntry;

typedef struct{
    ProfilingEntry * log;
    uint32_t count;
    uint32_t capacity;
} ProfilingLog;

int64_t get_profiler_frequency(void);

ProfilingLog * access_profiling_log(Renderer * r);

BitmapBgra * create_bitmap_bgra(Context * context, int sx, int sy, bool zeroed, BitmapPixelFormat format);
BitmapBgra * create_bitmap_bgra_header(Context * context, int sx, int sy);

RenderDetails * create_render_details(void);

Renderer * create_renderer(BitmapBgra * source, BitmapBgra * canvas, RenderDetails * details);
Renderer * create_renderer_in_place(BitmapBgra * editInPlace, RenderDetails * details);
int perform_render(Context * context, Renderer * r);
void destroy_renderer(Renderer * r);
void destroy_bitmap_bgra(BitmapBgra * im);

//These filters are stored in a struct as function pointers, which I assume means they can't be inlined. Likely 5 * w * h invocations.
double filter_flex_cubic(const InterpolationDetails * d, double x);
double filter_bicubic_fast(const InterpolationDetails * d, double t);
double filter_sinc_2(const InterpolationDetails * d, double t);
double filter_box(const InterpolationDetails * d, double t);
double filter_triangle(const InterpolationDetails * d, double t);
double filter_sinc_windowed(const InterpolationDetails * d, double t);
double percent_negative_weight(const InterpolationDetails * details);


InterpolationDetails * create_interpolation_details(void);
InterpolationDetails * create_bicubic_custom(double window, double blur, double B, double C);
InterpolationDetails * create_custom(double window, double blur, detailed_interpolation_method filter);
InterpolationDetails * create_interpolation(InterpolationFilter filter);
void destroy_interpolation_details(InterpolationDetails *);


uint32_t BitmapPixelFormat_bytes_per_pixel (BitmapPixelFormat format);

typedef struct {
    float *Weights;/* Normalized weights of neighboring pixels */
    int Left;      /* Bounds of source pixels window */
    int Right;
} ContributionType;/* Contirbution information for a single pixel */

typedef struct {
    ContributionType *ContribRow; /* Row (or column) of contribution weights */
    uint32_t WindowSize;      /* Filter window size (of affecting source pixels) */
    uint32_t LineLength;      /* Length of line (no. or rows / cols) */
    double percent_negative; /* Estimates the sharpening effect actually applied*/
} LineContribType;

LineContribType * contributions_calc(const uint32_t line_size, const uint32_t src_size, const InterpolationDetails * details);
void contributions_free(LineContribType * p);

// do these need to be public??
void free_lookup_tables(void);
LookupTables * get_lookup_tables(void);


ConvolutionKernel * create_convolution_kernel(uint32_t radius);
void free_convolution_kernel(ConvolutionKernel * kernel);


ConvolutionKernel* create_guassian_kernel(double stdDev, uint32_t radius);
double sum_of_kernel(ConvolutionKernel* kernel);
void normalize_kernel(ConvolutionKernel* kernel, float desiredSum);
ConvolutionKernel* create_guassian_kernel_normalized(double stdDev, uint32_t radius);
ConvolutionKernel* create_guassian_sharpen_kernel(double stdDev, uint32_t radius);

#ifdef __cplusplus
}
#endif
