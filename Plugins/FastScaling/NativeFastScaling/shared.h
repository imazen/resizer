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

#include "ir_alloc.h"
#include "math_functions.h"
#include "bitmap_formats.h"
#include "color_spaces.h"
#include "convolution.h"


enum Rotate{
    RotateNone = 0,
    Rotate90 = 1,
    Rotate180 = 2,
    Rotate270 = 3
};

int64_t get_high_precision_ticks(void);

#define ALLOW_PROFILING



#ifdef ALLOW_PROFILING
#define prof_start(r, name, allow_recursion) if (r->log != NULL){ profiler_start(r,name,allow_recursion);}
#define prof_stop(r, name, assert_started, stop_children) if (r->log != NULL){ profiler_stop(r,name,assert_started, stop_children);}
#else
#define prof_start(r, name, allow_recursion)
#define prof_stop(r, name, assert_started, stop_children)
#endif


void profiler_start(Renderer * r, const char * name, bool allow_recursion);
void profiler_stop(Renderer * r, const char * name, bool assert_started, bool stop_children);


void apply_color_matrix_float(BitmapFloat * bmp, const uint32_t row, const uint32_t count, float*  m[5]);
void apply_color_matrix(BitmapBgra * bmp, const uint32_t row, const uint32_t count, float* const __restrict  m[5]);

