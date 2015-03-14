/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */
#pragma once

#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>

#include "fastscaling.h"


typedef enum ColorSpaceEnum {
    ColorSpace_None = 0,
    ColorSpace_sRGB_BGR = 1,
    ColorSpace_RGBLinear_BGR = 2,
    ColorSpace_LUV = 3,
    ColorSpace_XYZ_YXZ = 4,
    ColorSpace_Sigmoid = 5
} ColorSpace;

// non-indexed bitmap
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
    //The color space currently in use
    ColorSpace color;
    //If true, alpha has been premultiplied
    bool alpha_premultiplied;
    //If true, the alpha channel holds meaningful data
    bool alpha_meaningful;
} BitmapFloat;


#ifdef __cplusplus
extern "C" {
#endif 
BitmapFloat * CreateBitmapFloatHeader(int sx, int sy, int channels);

BitmapFloat * CreateBitmapFloat(int sx, int sy, int channels, bool zeroed);

void DestroyBitmapFloat(BitmapFloat * im);
#ifdef __cplusplus
}
#endif
