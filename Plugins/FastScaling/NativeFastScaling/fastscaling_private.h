/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */
#pragma once
#ifdef _MSC_VER
#pragma unmanaged
#endif

#include "fastscaling.h"
#include "ir_alloc.h"
#include "math_functions.h"
#include "color.h"



#ifdef __cplusplus
extern "C" {
#endif

 //floating-point bitmap, typically linear RGBA, premultiplied
typedef struct BitmapFloatStruct {
    //buffer width in pixels
    uint32_t w;
    //buffer height in pixels
    uint32_t h;
    //The number of floats per pixel
    uint32_t channels;
    //The pixel data
    float *pixels;
    //If true, don't dispose the buffer with the struct
    bool pixels_borrowed;
    //The number of floats in the buffer
    uint32_t float_count;
    //The number of floats betwen (0,0) and (0,1)
    uint32_t float_stride;

    //If true, alpha has been premultiplied
    bool alpha_premultiplied;
    //If true, the alpha channel holds meaningful data
    bool alpha_meaningful;
} BitmapFloat;

#define ALLOW_PROFILING

#ifdef ALLOW_PROFILING
#define prof_start(r, name, allow_recursion) if (r->log != NULL){ profiler_start(r,name,allow_recursion);}
#define prof_stop(r, name, assert_started, stop_children) if (r->log != NULL){ profiler_stop(r,name,assert_started, stop_children);}
#else
#define prof_start(r, name, allow_recursion)
#define prof_stop(r, name, assert_started, stop_children)
#endif


BitmapFloat * create_bitmap_floatHeader(int sx, int sy, int channels);

BitmapFloat * create_bitmap_float(int sx, int sy, int channels, bool zeroed);

void destroy_bitmap_float(BitmapFloat * im);

int64_t get_high_precision_ticks(void);


void profiler_start(Renderer * r, const char * name, bool allow_recursion);
void profiler_stop(Renderer * r, const char * name, bool assert_started, bool stop_children);



void ScaleBgraFloatRows(BitmapFloat * from, uint32_t from_row, BitmapFloat * to, uint32_t to_row, uint32_t row_count, ContributionType * weights);
int ConvolveBgraFloatInPlace(BitmapFloat * buf, ConvolutionKernel *kernel,  uint32_t convolve_channels, uint32_t from_row, int row_count);
void SharpenBgraFloatRowsInPlace(BitmapFloat * im, uint32_t start_row, uint32_t row_count, double pct);


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

// int as divisior???
int Halve(const BitmapBgra * from, const BitmapBgra * to, int divisor);


// is it correct to use an int as the divisor here?
int HalveInPlace(BitmapBgra * from, int divisor);

#ifdef __cplusplus
}
#endif

