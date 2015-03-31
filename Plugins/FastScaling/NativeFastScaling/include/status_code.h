#ifndef STATUS_CODE_NAME
#define STATUS_CODE_NAME StatusCode
#define STATUS_CODE_ENUM_NAME typedef enum  _StatusCode
#endif
STATUS_CODE_ENUM_NAME {
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

} STATUS_CODE_NAME;
