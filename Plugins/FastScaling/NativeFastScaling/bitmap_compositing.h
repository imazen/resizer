#pragma once

#include "fastscaling.h"

#ifdef __cplusplus
extern "C" {
#endif

extern int convert_srgb_to_linear(
    BitmapBgra * src, 
    uint32_t from_row, 
    BitmapFloat * dest, 
    uint32_t dest_row, 
    uint32_t row_count);

extern int pivoting_composite_linear_over_srgb(
    BitmapFloat * src, 
    uint32_t from_row, 
    BitmapBgra * dest, 
    uint32_t dest_row, 
    uint32_t row_count, 
    bool transpose);

extern int vertical_flip_bgra(BitmapBgra * b);

#ifdef __cplusplus
}
#endif
