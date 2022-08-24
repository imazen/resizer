#ifndef STATUS_CODE_NAME
#define STATUS_CODE_NAME StatusCode
#endif

#ifndef FLOATSPACE_NAME
#define FLOATSPACE_NAME WorkingFloatspace
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

Filter_RobidouxFast = 1,
Filter_Robidoux = 2,
Filter_RobidouxSharp = 3,
Filter_Ginseng = 4,
Filter_GinsengSharp = 5,
Filter_Lanczos = 6,
Filter_LanczosSharp = 7,
Filter_Lanczos2 = 8,
Filter_Lanczos2Sharp = 9,
Filter_CubicFast = 10,
Filter_Cubic = 11,
Filter_CubicSharp = 12,
Filter_CatmullRom = 13,
Filter_Mitchell = 14,


Filter_CubicBSpline = 15,
Filter_Hermite = 16,
Filter_Jinc = 17,
Filter_RawLanczos3 = 18,
Filter_RawLanczos3Sharp = 19,
Filter_RawLanczos2 = 20,
Filter_RawLanczos2Sharp = 21,
Filter_Triangle = 22,
Filter_Linear = 23,
Filter_Box = 24,
Filter_CatmullRomFast = 25,
Filter_CatmullRomFastSharp = 26,

Filter_Fastest = 27,

Filter_MitchellFast = 28,
Filter_NCubic = 29,
Filter_NCubicSharp = 30

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
ENUM_START (FLOATSPACE_NAME, _WorkingFloatspace)
    Floatspace_srgb = 0,
    Floatspace_as_is = 0,
    Floatspace_linear = 1,
    Floatspace_gamma = 2
#ifdef EXPOSE_SIGMOID

    ,Floatspace_sigmoid = 4,
    Floatspace_gamma_sigmoid = 6,//2 | 4,

    Floatspace_sigmoid_2 = 12,//4 | 8,
    Floatspace_gamma_sigmoid_2 = 14,//4 | 8 | 2,

    Floatspace_sigmoid_3 = 20,//4 | 16,
    Floatspace_gamma_sigmoid_3 = 22,//4 | 16 | 2,
#endif

ENUM_END (FLOATSPACE_NAME)

#undef ENUM_START
#undef ENUM_END
#undef query_alias
