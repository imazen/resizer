#ifndef STATUS_CODE_NAME
#define STATUS_CODE_NAME StatusCode
#endif

#ifndef FLOATSSPACE_NAME
#define FLOATSSPACE_NAME WorkingFloatspace
#endif


#ifdef FASTSCALING_ENUMS_MANAGED
#pragma managed
#define ENUM_START(name, raw_name) public enum class name {
#else
#define ENUM_START(name,raw_name) typedef enum raw_name {
#endif

#ifdef FASTSCALING_ENUMS_MANAGED
#define ENUM_END(name) };
#else
#define ENUM_END(name) }name;
#endif


ENUM_START (STATUS_CODE_NAME, _StatusCode)
    No_Error = 0,
    Out_of_memory = 1,
    Invalid_BitmapBgra_dimensions,
    Invalid_BitmapFloat_dimensions,
    Unsupported_pixel_format,
    Invalid_internal_state,
    Transpose_not_permitted_in_place,
    Invalid_interpolation_filter,
    Invalid_argument,
    Interpolation_details_missing,
ENUM_END (STATUS_CODE_NAME)

#ifdef FASTSCALING_ENUMS_MANAGED
[ImageResizer::ExtensionMethods::EnumRemovePrefixAttribute ("Filter_")]
#endif
ENUM_START (InterpolationFilter, _InterpolationFilter)
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
    Filter_CatmullRomFastSharp,
    Filter_MitchellFast,
    Filter_Ginseng,
    Filter_Jinc,
    Filter_RobidouxFast
ENUM_END (InterpolationFilter)

ENUM_START (ProfilingEntryFlags, _ProfilingEntryFlags)
    Profiling_start = 2,
    Profiling_start_allow_recursion = 6,//2 | 4,
    Profiling_stop = 8,
    Profiling_stop_assert_started = 24,//8 | 16,
    Profiling_stop_children = 56//8 | 16 | 32,
ENUM_END (ProfilingEntryFlags)


//Compact format for bitmaps. sRGB or gamma adjusted - *NOT* linear
ENUM_START (BitmapPixelFormat,_BitmapPixelFormat)
    Bgr24 = 3,
    Bgra32 = 4,
    Gray8 = 1
ENUM_END (BitmapPixelFormat)


ENUM_START (BitmapCompositingMode, _BitmapCompositingMode)
    Replace_self = 0,
    Blend_with_self = 1,
    Blend_with_matte = 2
ENUM_END (BitmapCompositingMode)

#ifdef FASTSCALING_ENUMS_MANAGED
[ImageResizer::ExtensionMethods::EnumRemovePrefixAttribute ("Floatspace_")]
#endif
ENUM_START (FLOATSSPACE_NAME, _WorkingFloatspace)
Floatspace_auto = -1,
    Floatspace_as_is = 0,
    Floatspace_srgb_to_linear = 1,

    Floatspace_sigmoid = 2,
    Floatspace_srgb_to_sigmoid = 3,

    Floatspace_sigmoid_2 = 6,//2 | 4,
    Floatspace_srgb_to_sigmoid_2 = 7,//1 | 2 | 4,

    Floatspace_sigmoid_3 = 10,//2 | 8,
    Floatspace_srgb_to_sigmoid_3 = 11,//1 | 2 | 8,

    Floatspace_gamma = 32

    ENUM_END (FLOATSSPACE_NAME)

#undef ENUM_START
#undef ENUM_END
