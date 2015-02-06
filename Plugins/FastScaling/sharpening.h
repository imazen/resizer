#include "Stdafx.h"
#pragma once
#pragma unmanaged
#include "shared.h"



static void BgraSharpenInPlaceX(BitmapBgraPtr im, int pct)
{
    int x, y, current, prev, next, i;

    const int sx = im->w;
    const int sy = im->h;
    const int stride = im->stride;
    const int bpp = im->bpp;
    const float outer_coeff = -pct / 400.0;
    const float inner_coeff = 1 - 2 * outer_coeff;

    if (pct <= 0 || im->w < 3 || bpp < 3) return;

    for (y = 0; y < sy; y++)
    {
        unsigned char *row = im->pixels + y * stride;
        for (current = bpp, prev = 0, next = bpp + bpp; next < stride; prev = current, current = next, next += bpp){
            //We never sharpen the alpha channel
            for (int i = 0; i < 3; i++)
                row[current + i] = uchar_clamp_ff(outer_coeff * (float)row[prev + i] + inner_coeff * (float)row[current + i] + outer_coeff * (float)row[next + i]);
        }
    }
}


static void
SharpenBgraFloatInPlace(float* buf, unsigned int count, double pct,
int step = 4){

    const float c_o = -pct / 400.0;
    const float c_i = 1 - 2 * c_o;

    unsigned int ndx;

    // if both have alpha, process it
    if (step == 4)
    {
        float left_b = buf[0 * 4 + 0];
        float left_g = buf[0 * 4 + 1];
        float left_r = buf[0 * 4 + 2];
        float left_a = buf[0 * 4 + 3];

        for (ndx = 1; ndx < count - 1; ndx++) {
            const float b = buf[ndx * 4 + 0];
            const float g = buf[ndx * 4 + 1];
            const float r = buf[ndx * 4 + 2];
            const float a = buf[ndx * 4 + 3];
            buf[ndx * 4 + 0] = left_b * c_o + b * c_i + buf[(ndx + 1) * 4 + 0] * c_o;
            buf[ndx * 4 + 1] = left_b * c_o + g * c_i + buf[(ndx + 1) * 4 + 1] * c_o;
            buf[ndx * 4 + 2] = left_b * c_o + r * c_i + buf[(ndx + 1) * 4 + 2] * c_o;
            buf[ndx * 4 + 3] = left_b * c_o + a * c_i + buf[(ndx + 1) * 4 + 3] * c_o;
            left_b = b;
            left_g = g;
            left_r = r;
            left_a = a;
        }
    }
    // otherwise do the same thing without 4th chan
    // (ifs in loops are expensive..)
    else
    {
        float left_b = buf[0 * 3 + 0];
        float left_g = buf[0 * 3 + 1];
        float left_r = buf[0 * 3 + 2];

        for (ndx = 1; ndx < count - 1; ndx++) {
            const float b = buf[ndx * 3 + 0];
            const float g = buf[ndx * 3 + 1];
            const float r = buf[ndx * 3 + 2];
            buf[ndx * 3 + 0] = left_b * c_o + b * c_i + buf[(ndx + 1) * 3 + 0] * c_o;
            buf[ndx * 3 + 1] = left_b * c_o + g * c_i + buf[(ndx + 1) * 3 + 1] * c_o;
            buf[ndx * 3 + 2] = left_b * c_o + r * c_i + buf[(ndx + 1) * 3 + 2] * c_o;
            left_b = b;
            left_g = g;
            left_r = r;
        }

    }

}




static void
ConvolveBgraFloatInPlace(float *source_buffer, unsigned int source_buffer_count, unsigned int source_buffer_len, const float *kernel, int radius,
int step = 4){

    unsigned int ndx;

    // if both have alpha, process it
    if (step == 4)
    {
        for (ndx = radius; ndx < source_buffer_count - radius; ndx++) {
            float r = 0, g = 0, b = 0, a = 0;
            const int left = ndx - radius;
            const int right = ndx + radius;
            int i;

            /* Accumulate each channel */
            for (i = left; i <= right; i++) {
                const float weight = kernel[i - left];

                b += weight * source_buffer[i * 4];
                g += weight * source_buffer[i * 4 + 1];
                r += weight * source_buffer[i * 4 + 2];
                a += weight * source_buffer[i * 4 + 3];
            }
            //Todo - add threshold
            source_buffer[ndx * 4] = b;
            source_buffer[ndx * 4 + 1] = g;
            source_buffer[ndx * 4 + 2] = r;
            source_buffer[ndx * 4 + 3] = a;
        }
    }
    // otherwise do the same thing without 4th chan
    // (ifs in loops are expensive..)
    else
    {
        for (ndx = radius; ndx < source_buffer_count - radius; ndx++) {
            float r = 0, g = 0, b = 0;
            const int left = ndx - radius;
            const int right = ndx + radius;
            int i;

            /* Accumulate each channel */
            for (i = left; i <= right; i++) {
                const float weight = kernel[i - left];

                b += weight * source_buffer[i * step];
                g += weight * source_buffer[i * step + 1];
                r += weight * source_buffer[i * step + 2];
            }

            source_buffer[ndx * step] = b;
            source_buffer[ndx * step + 1] = g;
            source_buffer[ndx * step + 2] = r;
        }
    }

}

