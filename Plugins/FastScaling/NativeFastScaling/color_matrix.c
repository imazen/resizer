/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */
#ifdef _MSC_VER
#pragma unmanaged
#endif

#include "fastscaling.h"
#include "math_functions.h"
#include "bitmap_formats.h"
#include "shared.h"


void apply_color_matrix(BitmapBgra * bmp, const uint32_t row, const uint32_t count, float* const __restrict  m[5])
{
    const uint32_t stride = bmp->stride;
    const uint32_t ch = bmp->bpp;
    const uint32_t w = bmp->w;
    const uint32_t h = MIN(row + count, bmp->h);
    if (ch == 4)
    {
        
        for (uint32_t y = row; y < h; y++)
            for (uint32_t x = 0; x < w; x++)
        {
            uint8_t* const __restrict data = bmp->pixels + stride * y + x * ch;

            const uint8_t r = uchar_clamp_ff(m[0][0] * data[2] + m[1][0] * data[1] + m[2][0] * data[0] + m[3][0] * data[3] + m[4][0]);
            const uint8_t g = uchar_clamp_ff(m[0][1] * data[2] + m[1][1] * data[1] + m[2][1] * data[0] + m[3][1] * data[3] + m[4][1]);
            const uint8_t b = uchar_clamp_ff(m[0][2] * data[2] + m[1][2] * data[1] + m[2][2] * data[0] + m[3][2] * data[3] + m[4][2]);
            const uint8_t a = uchar_clamp_ff(m[0][3] * data[2] + m[1][3] * data[1] + m[2][3] * data[0] + m[3][3] * data[3] + m[4][3]);

            uint8_t* newdata = bmp->pixels + stride * y + x * ch;
            newdata[0] = b;
            newdata[1] = g;
            newdata[2] = r;
            newdata[3] = a;
        }
    }
    else if (ch == 3)
    {
        
        for (uint32_t y = row; y < h; y++)
            for (uint32_t x = 0; x < w; x++)
        {
            unsigned char* const __restrict data = bmp->pixels + stride * y + x * ch;

            const uint8_t r = uchar_clamp_ff(m[0][0] * data[2] + m[1][0] * data[1] + m[2][0] * data[0] + m[4][0]);
            const uint8_t g = uchar_clamp_ff(m[0][1] * data[2] + m[1][1] * data[1] + m[2][1] * data[0] + m[4][1]);
            const uint8_t b = uchar_clamp_ff(m[0][2] * data[2] + m[1][2] * data[1] + m[2][2] * data[0] + m[4][2]);

            uint8_t* newdata = bmp->pixels + stride * y + x * ch;
            newdata[0] = b;
            newdata[1] = g;
            newdata[2] = r;
        }
    }
}


void apply_color_matrix_float(BitmapFloat * bmp, const uint32_t row, const uint32_t count, float*  m[5])
{
    const uint32_t stride = bmp->float_stride;
    const uint32_t ch = bmp->channels;
    const uint32_t w = bmp->w;
    const uint32_t h = MIN(row + count,bmp->h);
    if (ch == 4)
    {

        for (uint32_t y = row; y < h; y++)
            for (uint32_t x = 0; x < w; x++)
            {
                float* const __restrict data = bmp->pixels + stride * y + x * ch;

                const float r = (m[0][0] * data[2] + m[1][0] * data[1] + m[2][0] * data[0] + m[3][0] * data[3] + m[4][0]);
                const float g = (m[0][1] * data[2] + m[1][1] * data[1] + m[2][1] * data[0] + m[3][1] * data[3] + m[4][1]);
                const float b = (m[0][2] * data[2] + m[1][2] * data[1] + m[2][2] * data[0] + m[3][2] * data[3] + m[4][2]);
                const float a = (m[0][3] * data[2] + m[1][3] * data[1] + m[2][3] * data[0] + m[3][3] * data[3] + m[4][3]);

                float * newdata = bmp->pixels + stride * y + x * ch;
                newdata[0] = b;
                newdata[1] = g;
                newdata[2] = r;
                newdata[3] = a;

            }
    }
    else if (ch == 3)
    {

        for (uint32_t y = row; y < h; y++)
            for (uint32_t x = 0; x < w; x++)
            {
                
                float* const __restrict data = bmp->pixels + stride * y + x * ch;

                const float  r = data[2] = (m[0][0] * data[2] + m[1][0] * data[1] + m[2][0] * data[0] + m[4][0]);
                const float g = data[1] = (m[0][1] * data[2] + m[1][1] * data[1] + m[2][1] * data[0] + m[4][1]);
                const float b = data[0] = (m[0][2] * data[2] + m[1][2] * data[1] + m[2][2] * data[0] + m[4][2]);

                float * newdata = bmp->pixels + stride * y + x * ch;
                newdata[0] = b;
                newdata[1] = g;
                newdata[2] = r;
            }
    }
}
