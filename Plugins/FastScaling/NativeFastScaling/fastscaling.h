#pragma once


/**

  Currently we only support BGR24 and BGRA32 pixel formats. (And BGR32, where we ignore the alpha channel, but that's not precisely a separate format)

  Eventually we will need to support

  * 8-bit grayscale
  * CMYK
  * YCbCr

  and possibly others. For V1, the API we expose is only used by projects in the same repository, running under the same tests.

  In V2, we can change the API as we wish; we are not constrained to what we design here.

  Perhaps it is best to explicitly limit the structure to represent what we process at this time?

  If our buffers and structures actually describe their contents, then we need to support all permitted values in all functions. This is problematic.

  We heavily experimented with LUV and XYZ color spaces, but determined that better results occur using RGB linear.

  A custom sigmoidized color space could perhaps improve things, but would introduce significant overhead.
**/

//This kind of describes the API as-is, not as it should be

/* Proposed changes

combine BitmapBgraStruct->bpp and BitmapBgraStruct->pixel_format somehow.

Make  BitmapBgraStruct->matte_color a fixed 4 bytes sRGBA value, remove ->borrowed_matte_color

Drop ColorSpace. We assume sRGB for BitmapBgra, RGBLinear for BitmapFloat

Rename things for clarity.

Use const/restrict where appropriate

Refactor everything around convolution kernels; perhaps they should have their own struct?

*/

#include <stdint.h>
#include <stdbool.h>


#ifdef __cplusplus
extern "C" {
#endif

typedef enum _BitmapPixelFormat {
    None = 0,
    Bgr24 = 24,
    Bgra32 = 32,
    Gray8 = 8
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
    //TODO: rename to bytes_pp
    uint32_t bpp;
    //Todo - combine with bpp somehow. DRY this out
    BitmapPixelFormat pixel_format;

    //When using compositing mode blend_with_matte, this color will be used. We should probably define this as always being sRGBA, 4 bytes.
    unsigned char *matte_color;
    ///If true, we don't dispose of *pixels when we dispose the struct
    bool borrowed_matte_color;

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

    float sharpen_percent_goal;

} InterpolationDetails;

typedef struct RenderDetailsStruct{

    //Original scaling values, if required.
    //scale/scale_h are sans-transpose.
    //final_w/final_h is the actual result size expected afer all operations
    //uint32_t from_w, scale_w, final_w, from_h, final_h, scale_h;


    InterpolationDetails * interpolation;

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
    bool enable_profiling;

} RenderDetails;

typedef struct LookupTablesStruct *LookupTablesPtr;

typedef struct LookupTablesStruct {
    float srgb_to_linear[256]; //Converts 0..255 -> 0..1, but knowing that 0.255 has sRGB gamma.
    float linear[256]; //Converts 0..255 -> 0..1
    //const uint8_t linear_to_srgb[4097]; //Converts from 0..4096 to 0.255, going from linear to sRGB gamma.
} LookupTables;


typedef enum _ProfilingEntryFlags {
    Profiling_none = 0,
    Profiling_start_allow_recursion = 8,
    Profiling_stop_children = 4,
    Profiling_assert_started = 2

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

BitmapBgra * create_bitmap_bgra(int sx, int sy, bool zeroed, int bpp);
BitmapBgra * create_bitmap_bgra_header(int sx, int sy);

RenderDetails * create_render_details(void);

Renderer * create_renderer(BitmapBgra * source, BitmapBgra * canvas, RenderDetails * details);
int perform_render(Renderer * r);
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
    double percent_negative; /*estimates the sharpening effect*/
} LineContribType;

LineContribType * contributions_calc(const uint32_t line_size, const uint32_t src_size, const InterpolationDetails * details);
void contributions_free(LineContribType * p);

// do these need to be public??
void free_lookup_tables(void);
LookupTables * get_lookup_tables(void);



float* create_guassian_kernel(double stdDev, uint32_t radius);
double sum_of_kernel(float* kernel, uint32_t size);
void normalize_kernel(float* kernel, uint32_t size, float desiredSum);
float* create_guassian_kernel_normalized(double stdDev, uint32_t radius);
float* create_guassian_sharpen_kernel(double stdDev, uint32_t radius);

#ifdef __cplusplus
}
#endif
