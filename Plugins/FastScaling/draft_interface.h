
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

enum ColorSpace{
    ColorSpace_None = 0,
    ColorSpace_sRGB_BGR = 1,
    ColorSpace_RGBLinear_BGR = 2,
    ColorSpace_LUV = 3,
    ColorSpace_XYZ_YXZ = 4,
    ColorSpace_Sigmoid = 5
};

typedef struct BitmapBgraStruct *BitmapBgraPtr;

//non-indexed bitmap
typedef struct BitmapBgraStruct{

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

    //When using compositing mode blend_with_matte, this color will be used. We should probably define this as always being sRGBA 
    unsigned char *matte_color;
    ///If true, we don't dispose of *pixels when we dispose the struct
    bool borrowed_matte_color;

    BitmapCompositingMode compositing_mode;

} BitmapBgra;


static int vertical_flip_bgra(BitmapBgraPtr b);
static int copy_bitmap_bgra(BitmapBgraPtr src, BitmapBgraPtr dst);
static BitmapBgraPtr CreateBitmapBgraHeader(int sx, int sy);
static BitmapBgraPtr CreateBitmapBgra(int sx, int sy, bool zeroed, int bpp);
inline void DestroyBitmapBgra(BitmapBgraPtr im);

float srgb_to_linear(float s);
float linear_to_srgb(float s);
static inline uint8_t uchar_clamp_ff(float clr);



float* create_guassian_kernel(double stdDev, uint32_t radius);
void normalize_kernel(float* kernel, uint32_t size, float desiredSum);
float* create_guassian_kernel_normalized(double stdDev, uint32_t radius);
float* create_guassian_sharpen_kernel(double stdDev, uint32_t radius);

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
    Filter_CubicBSpline,
    Filter_Lanczos3Windowed,
    Filter_Lanczos3SharpWindowed,
    Filter_Lanczos2Windowed,
    Filter_Lanczos2SharpWindowed,
    Filter_CatmullRomFast,
    Filter_CatmullRomFastSharp
};

enum Rotate{
    RotateNone = 0,
    Rotate90 = 1,
    Rotate180 = 2,
    Rotate270 = 3
};


typedef struct InterpolationDetailsStruct *InterpolationDetailsPtr;

typedef double(*detailed_interpolation_method)(InterpolationDetailsPtr, double);

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
    double percent_negative; /*estimates the sharpening effect*/
} LineContribType;



typedef struct RenderDetailsStruct *RenderDetailsPtr;


typedef struct RenderDetailsStruct{

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
 
} RenderDetails;

RendererPtr CreateRenderer(BitmapBgraPtr editInPlace, RenderDetailsPtr details);

RendererPtr CreateRenderer(BitmapBgraPtr source, BitmapBgraPtr canvas, RenderDetailsPtr details);

void DestroyRenderer(RendererPtr r)

static InterpolationDetailsPtr CreateInterpolationDetails();

static RenderDetailsPtr CreateRenderDetails();
static void DestroyRenderDetails(RenderDetailsPtr d);
static void FreeLookupTables() ;
static LookupTablesPtr GetLookupTables();

static void BgraSharpenInPlaceX(BitmapBgraPtr im, float pct);


typedef struct Rect{
    uint32_t x1, y1, x2, y2;

}RectStruct;

//Sobel whitespace trimming - untested
static Rect detect_content(BitmapBgraPtr b, uint8_t threshold);

typedef struct RendererStruct *RendererPtr;

int PerformRender(RendererPtr r);



//These filters are stored in a struct as function pointers, which I assume means they can't be inlined. Likely 5 * w * h invocations.



static double filter_flex_cubic(const InterpolationDetailsPtr d, double x);
static double filter_bicubic_fast(const InterpolationDetailsPtr d, const double t);


static double filter_sinc_2(const InterpolationDetailsPtr d, double t);

static double filter_box(const InterpolationDetailsPtr d, double t);

static double filter_triangle(const InterpolationDetailsPtr d, double t);
static double filter_sinc_windowed(const InterpolationDetailsPtr d, double t);



static InterpolationDetailsPtr CreateBicubicCustom(double window, double blur, double B, double C);
static InterpolationDetailsPtr CreateCustom(double window, double blur, detailed_interpolation_method filter);
static InterpolationDetailsPtr CreateInterpolation(InterpolationFilter filter);static void ContributionsFree(LineContribType * p);
static double percent_negative_weight(const InterpolationDetailsPtr details);

static LineContribType *ContributionsCalc(const uint32_t line_size, const uint32_t src_size, const InterpolationDetailsPtr details);



