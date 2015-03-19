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
#define prof_start(context, name, allow_recursion)  Context_profiler_start(context,name,allow_recursion);
#define prof_stop(context, name, assert_started, stop_children) Context_profiler_stop(context,name,assert_started, stop_children);
#else
#define prof_start(context, name, allow_recursion)
#define prof_stop(context, name, assert_started, stop_children)
#endif

void Context_profiler_start(Context * context, const char * name, bool allow_recursion);
void Context_profiler_stop(Context * context, const char * name, bool assert_started, bool stop_children);

BitmapFloat * BitmapFloat_create_header(Context * context, int sx, int sy, int channels);

BitmapFloat * BitmapFloat_create(Context * context, int sx, int sy, int channels, bool zeroed);

void BitmapFloat_destroy(Context * context, BitmapFloat * im);

bool BitmapFloat_scale_rows(Context * context, BitmapFloat * from, uint32_t from_row, BitmapFloat * to, uint32_t to_row, uint32_t row_count, PixelContributions * weights);
bool BitmapFloat_convolve_rows(Context * context, BitmapFloat * buf, ConvolutionKernel *kernel,  uint32_t convolve_channels, uint32_t from_row, int row_count);

bool BitmapFloat_sharpen_rows(Context * context, BitmapFloat * im, uint32_t start_row, uint32_t row_count, double pct);


bool BitmapBgra_convert_srgb_to_linear(Context * context,
    BitmapBgra * src,
    uint32_t from_row,
    BitmapFloat * dest,
    uint32_t dest_row,
    uint32_t row_count);

bool BitmapFloat_pivoting_composite_linear_over_srgb(Context * context,
    BitmapFloat * src,
    uint32_t from_row,
    BitmapBgra * dest,
    uint32_t dest_row,
    uint32_t row_count,
    bool transpose);

bool BitmapBgra_flip_vertical(Context * context, BitmapBgra * b);

bool BitmapFloat_demultiply_alpha(
    Context * context,
    BitmapFloat * src,
    const uint32_t from_row,
    const uint32_t row_count);

bool BitmapFloat_copy_linear_over_srgb(
    Context * context,
    BitmapFloat * src,
    const uint32_t from_row,
    BitmapBgra * dest,
    const uint32_t dest_row,
    const uint32_t row_count,
    const uint32_t from_col,
    const uint32_t col_count,
    const bool transpose);

bool Halve(Context * context, const BitmapBgra * from, BitmapBgra * to, int divisor);

bool HalveInPlace(Context * context, BitmapBgra * from, int divisor);



#ifndef _TIMERS_IMPLEMENTED
#define _TIMERS_IMPLEMENTED
#ifdef _WIN32
    #define STRICT
    #define WIN32_LEAN_AND_MEAN
    #include <windows.h>
    #include <winbase.h>
    inline int64_t get_high_precision_ticks(void){
        LARGE_INTEGER val;
        QueryPerformanceCounter(&val);
        return val.QuadPart;
    }
    inline int64_t get_profiler_ticks_per_second(void){
        LARGE_INTEGER val;
        QueryPerformanceFrequency(&val);
        return val.QuadPart;
    }
#else
    #include <sys/time.h>
    #if defined(_POSIX_VERSION)
    #if defined(_POSIX_TIMERS) && (_POSIX_TIMERS > 0)
    #if defined(CLOCK_MONOTONIC_PRECISE)
            /* BSD. --------------------------------------------- */
            #define PROFILER_CLOCK_ID id = CLOCK_MONOTONIC_PRECISE;
    #elif defined(CLOCK_MONOTONIC_RAW)
            /* Linux. ------------------------------------------- */
            #define PROFILER_CLOCK_ID id = CLOCK_MONOTONIC_RAW;
    #elif defined(CLOCK_HIGHRES)
            /* Solaris. ----------------------------------------- */
            #define PROFILER_CLOCK_ID id = CLOCK_HIGHRES;
    #elif defined(CLOCK_MONOTONIC)
            /* AIX, BSD, Linux, POSIX, Solaris. ----------------- */
            #define PROFILER_CLOCK_ID id = CLOCK_MONOTONIC;
    #elif defined(CLOCK_REALTIME)
            /* AIX, BSD, HP-UX, Linux, POSIX. ------------------- */
            #define PROFILER_CLOCK_ID id = CLOCK_REALTIME;
    #endif
    #endif
    #endif


    inline int64_t get_high_precision_ticks(void){
        #ifdef PROFILER_CLOCK_ID
            timespec ts;
            if (clock_gettime(PROFILER_CLOCK_ID, &ts) != 0){
                return -1;
            }
            return ts->tv_sec * 1000000 +  ts->tv_nsec;
        #else
            struct timeval tm;
            if (gettimeofday( &tm, NULL) != 0){
                return -1;
            }
            return tm.tv_sec * 1000000 + tm.tv_usec;
        #endif
    }

    inline int64_t get_profiler_ticks_per_second(void){
        #ifdef PROFILER_CLOCK_ID
            timespec ts;
            if (clock_getres(PROFILER_CLOCK_ID, &ts) != 0){
                return -1;
            }
            return ts->tv_nsec;
        #else
            return 1000000;
        #endif
    }

#endif
#endif


#ifdef __cplusplus
}
#endif

