#pragma once

#include "fastscaling.h"

#ifdef __cplusplus
extern "C" {
#endif

int convert_srgb_to_linear(
    BitmapBgra * src, 
    uint32_t from_row, 
    BitmapFloat * dest, 
    uint32_t dest_row, 
    uint32_t row_count);

int pivoting_composite_linear_over_srgb(
    BitmapFloat * src, 
    uint32_t from_row, 
    BitmapBgra * dest, 
    uint32_t dest_row, 
    uint32_t row_count, 
    bool transpose);

int vertical_flip_bgra(BitmapBgra * b);

void demultiply_alpha(
    BitmapFloat * src, 
    const uint32_t from_row, 
    const uint32_t row_count);

void copy_linear_over_srgb(
    BitmapFloat * src, 
    const uint32_t from_row, 
    BitmapBgra * dest, 
    const uint32_t dest_row, 
    const uint32_t row_count, 
    const uint32_t from_col, 
    const uint32_t col_count, 
    const bool transpose);

#ifdef __cplusplus
}
#endif
