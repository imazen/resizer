#pragma once

#include <stdint.h>
#include <stdbool.h>

#ifdef LIBRARY_EXPORTS
#  define FASTSCALING_API __declspec(dllimport)
#else
#  define FASTSCALING_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef enum  {
    None = 0,
    Bgr24 = 24,
    Bgra32 = 32,
    Gray8 = 8
} BitmapPixelFormat;

typedef enum {
    Replace_self = 0,
    Blend_with_self = 1,
    Blend_with_matte = 2
} BitmapCompositingMode;

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

    BitmapPixelFormat pixel_format;

    //When using compositing mode blend_with_matte, this color will be used
    unsigned char *matte_color;
    ///If true, we don't dispose of *pixels when we dispose the struct
    bool borrowed_matte_color;

    BitmapCompositingMode compositing_mode;

} BitmapBgra;

FASTSCALING_API BitmapBgra * CreateBitmapBgra(int sx, int sy, bool zeroed, int bpp);

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
 
} RenderDetails;

FASTSCALING_API RenderDetails * CreateRenderDetails(void);
FASTSCALING_API InterpolationDetails * CreateInterpolationDetails(void);
FASTSCALING_API InterpolationDetails * CreateInterpolation(InterpolationFilter filter);
FASTSCALING_API Renderer * CreateRenderer(BitmapBgra * source, BitmapBgra * canvas, RenderDetails * details);
FASTSCALING_API int PerformRender(Renderer * r);
FASTSCALING_API void DestroyRenderer(Renderer * r);
FASTSCALING_API void DestroyBitmapBgra(BitmapBgra * im);

typedef struct LookupTablesStruct *LookupTablesPtr;

typedef struct LookupTablesStruct {
    const float srgb_to_linear[256]; //Converts 0..255 -> 0..1, but knowing that 0.255 has sRGB gamma.
    const float linear[256]; //Converts 0..255 -> 0..1
    //const uint8_t linear_to_srgb[4097]; //Converts from 0..4096 to 0.255, going from linear to sRGB gamma.
} LookupTables;

FASTSCALING_API void FreeLookupTables();
FASTSCALING_API LookupTables * GetLookupTables();


#ifdef __cplusplus
}
#endif
