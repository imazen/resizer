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



void apply_color_matrix_float(BitmapFloat * bmp, const uint32_t row, const uint32_t count, float*  m[5]);
void apply_color_matrix(BitmapBgra * bmp, const uint32_t row, const uint32_t count, float* const __restrict  m[5]);

