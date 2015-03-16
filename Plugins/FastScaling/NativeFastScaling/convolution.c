/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */

#include "fastscaling_private.h"
#include "bitmap_formats.h"
#include "math_functions.h"
#include <string.h>

#ifndef _MSC_VER
#include <alloca.h>
#else
#pragma unmanaged
#ifndef alloca
#include <malloc.h>
#define alloca _alloca
#endif
#endif


float* create_guassian_kernel(double stdDev, uint32_t radius){
    uint32_t size = radius * 2 + 1;
    float *kernel = (float *)malloc(sizeof(float) * size);
    if (kernel == NULL) return NULL;
    for (uint32_t i = 0; i < size; i++){
        kernel[i] = (float)(IR_GUASSIAN(fabs((float)(radius - i)), stdDev));
    }
    return kernel;
}

double sum_of_kernel(float* kernel, uint32_t size){
    double sum = 0;
    for (uint32_t i = 0; i < size; i++){
        sum += kernel[i];
    }
    return sum;
}

void normalize_kernel(float* kernel, uint32_t size, float desiredSum){
    float factor = (float)(desiredSum / sum_of_kernel(kernel,size));
    for (uint32_t i = 0; i < size; i++){
        kernel[i] *= factor;
    }
}
 float* create_guassian_kernel_normalized(double stdDev, uint32_t radius){
    float *kernel = create_guassian_kernel(stdDev, radius);
    if (kernel == NULL) return NULL;
    uint32_t size = radius * 2 + 1;
    normalize_kernel(kernel, size, 1);
    return kernel;
}

 float* create_guassian_sharpen_kernel(double stdDev, uint32_t radius){
    float *kernel = create_guassian_kernel(stdDev, radius);
    if (kernel == NULL) return NULL;
    uint32_t size = radius * 2 + 1;
    double sum = sum_of_kernel(kernel, size);

    for (uint32_t i = 0; i < size; i++){
        if (i == radius){
            kernel[i] = (float)(2 * sum - kernel[i]);
        }
        else{
            kernel[i] *= -1;
        }
    }
    normalize_kernel(kernel, size,1);
    return kernel;
}


int ConvolveBgraFloatInPlace(BitmapFloat * buf, const float *kernel, uint32_t radius, float threshold_min, float threshold_max, uint32_t convolve_channels, uint32_t from_row, int row_count)
{

    if (buf->w < radius + 1) return -2; //Do nothing unless the image is at least half as wide as the kernel.

    const uint32_t buffer_count = radius + 1;
    const uint32_t w = buf->w;
    const uint32_t step = buf->channels;

    const uint32_t until_row = row_count < 0 ? buf->h : from_row + (unsigned)row_count;

    const uint32_t ch_used = convolve_channels;

    float* __restrict buffer = (float *)alloca(sizeof(float) * buffer_count * ch_used);
    if (buffer == NULL) return -1;
    float* __restrict avg = (float *)alloca(sizeof(float) * ch_used);
    if (avg == NULL) {
        return -1;
    }


    for (uint32_t row = from_row; row < until_row; row++){

        float* __restrict source_buffer = &buf->pixels[row * buf->float_stride];
        int circular_idx = 0;

        for (uint32_t ndx = 0; ndx < w + buffer_count; ndx++) {
            //Flush old value
            if (ndx >= buffer_count){
                memcpy(&source_buffer[(ndx - buffer_count) * step], &buffer[circular_idx * ch_used], ch_used * sizeof(float));
            }
            //Calculate and enqueue new value
            if (ndx < w){
                const int left = ndx - radius;
                const int right = ndx + radius;
                int i;

                memset(avg, 0, sizeof(float) * ch_used);

                if (left < 0 || right >= (int32_t)w){
                    /* Accumulate each channel */
                    for (i = left; i <= right; i++) {
                        const float weight = kernel[i - left];
                        const uint32_t ix = CLAMP(i, 0, (int32_t)w);
                        for (uint32_t j = 0; j < ch_used; j++)
                            avg[j] += weight * source_buffer[ix * step + j];
                    }
                }
                else{
                    /* Accumulate each channel */
                    for (i = left; i <= right; i++) {
                        const float weight = kernel[i - left];
                        for (uint32_t j = 0; j < ch_used; j++)
                            avg[j] += weight * source_buffer[i * step + j];
                    }
                }

                //Enqueue difference
                memcpy(&buffer[circular_idx * ch_used], avg, ch_used * sizeof(float));

                if (threshold_min > 0 || threshold_max > 0){
                    float change = 0;
                    for (uint32_t j = 0; j < ch_used; j++)
                        change += (float)fabs(source_buffer[ndx * step + j] - avg[j]);

                    if (change < threshold_min || change > threshold_max){
                        memcpy(&buffer[circular_idx * ch_used], &source_buffer[ndx * step], ch_used * sizeof(float));
                    }
                }
            }
            circular_idx = (circular_idx + 1) % buffer_count;

        }
    }
    return 0;
}



static void BgraSharpenInPlaceX(BitmapBgra * im, float pct)
{
    const float n = -pct / (pct - 1); //if 0 < pct < 1
    const float outer_coeff = n / -2.0f;
    const float inner_coeff = n + 1;

    uint32_t y, current, prev, next;

    const uint32_t sy = im->h;
    const uint32_t stride = im->stride;
    const uint32_t bpp = im->bpp;


    if (pct <= 0 || im->w < 3 || bpp < 3) return;

    for (y = 0; y < sy; y++)
    {
        unsigned char *row = im->pixels + y * stride;
        for (current = bpp, prev = 0, next = bpp + bpp; next < stride; prev = current, current = next, next += bpp){
            //We never sharpen the alpha channel
            //TODO - we need to buffer the left pixel to prevent it from affecting later calculations
            for (uint32_t i = 0; i < 3; i++)
                row[current + i] = uchar_clamp_ff(outer_coeff * (float)row[prev + i] + inner_coeff * (float)row[current + i] + outer_coeff * (float)row[next + i]);
        }
    }
}


static void
SharpenBgraFloatInPlace(float* buf, unsigned int count, double pct,
int step)
{

    const float n = (float)(-pct / (pct - 1)); //if 0 < pct < 1
    const float c_o = n / -2.0f;
    const float c_i = n + 1;

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
            buf[ndx * 4 + 1] = left_g * c_o + g * c_i + buf[(ndx + 1) * 4 + 1] * c_o;
            buf[ndx * 4 + 2] = left_r * c_o + r * c_i + buf[(ndx + 1) * 4 + 2] * c_o;
            buf[ndx * 4 + 3] = left_a * c_o + a * c_i + buf[(ndx + 1) * 4 + 3] * c_o;
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
            buf[ndx * 3 + 1] = left_g * c_o + g * c_i + buf[(ndx + 1) * 3 + 1] * c_o;
            buf[ndx * 3 + 2] = left_r * c_o + r * c_i + buf[(ndx + 1) * 3 + 2] * c_o;
            left_b = b;
            left_g = g;
            left_r = r;
        }

    }

}





void
SharpenBgraFloatRowsInPlace(BitmapFloat * im, uint32_t start_row, uint32_t row_count, double pct){
    for (uint32_t row = start_row; row < start_row + row_count; row++){
        SharpenBgraFloatInPlace(im->pixels + (im->float_stride * row), im->w, pct, im->channels);
    }
}
