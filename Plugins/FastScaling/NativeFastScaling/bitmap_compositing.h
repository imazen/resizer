#pragma once

#ifdef __cplusplus
extern "C" {
#endif

extern int convert_srgb_to_linear(
    BitmapBgraPtr src, 
    uint32_t from_row, 
    BitmapFloatPtr dest, 
    uint32_t dest_row, 
    uint32_t row_count);

extern int pivoting_composite_linear_over_srgb(
    BitmapFloatPtr src, 
    uint32_t from_row, 
    BitmapBgraPtr dest, 
    uint32_t dest_row, 
    uint32_t row_count, 
    bool transpose);

extern int vertical_flip_bgra(BitmapBgraPtr b);

#ifdef __cplusplus
}
#endif
