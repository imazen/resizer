/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */

#include "scaling.h"
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


void ScaleBgraFloatRows(BitmapFloat * from, uint32_t from_row, BitmapFloat * to, uint32_t to_row, uint32_t row_count, ContributionType * weights)
{

    const uint32_t from_step = from->channels;
    const uint32_t to_step = to->channels;
    const uint32_t dest_buffer_count = to->w;



    for (uint32_t row = 0; row < row_count; row++)
    {
        const float* __restrict source_buffer = from->pixels + ((from_row + row) * from->float_stride);
        float* __restrict dest_buffer = to->pixels + ((to_row + row) * to->float_stride);

        uint32_t ndx;

        // if both have alpha, process it
        if (from_step == 4 && to_step == 4)
        {
            for (ndx = 0; ndx < dest_buffer_count; ndx++) {
                float r = 0, g = 0, b = 0, a = 0;
                const int left = weights[ndx].Left;
                const int right = weights[ndx].Right;

                const float* __restrict weightArray = weights[ndx].Weights;
                int i;

                /* Accumulate each channel */
                for (i = left; i <= right; i++) {
                    const float weight = weightArray[i - left];

                    b += weight * source_buffer[i * from_step];
                    g += weight * source_buffer[i * from_step + 1];
                    r += weight * source_buffer[i * from_step + 2];
                    a += weight * source_buffer[i * from_step + 3];
                }

                dest_buffer[ndx * to_step] = b;
                dest_buffer[ndx * to_step + 1] = g;
                dest_buffer[ndx * to_step + 2] = r;
                dest_buffer[ndx * to_step + 3] = a;
            }
        }
        else if (from_step == 3 && to_step == 3)
        {
            for (ndx = 0; ndx < dest_buffer_count; ndx++) {
                float r = 0, g = 0, b = 0;
                const int left = weights[ndx].Left;
                const int right = weights[ndx].Right;

                const float * weightArray = weights[ndx].Weights;
                int i;

                /* Accumulate each channel */
                for (i = left; i <= right; i++) {
                    const float weight = weightArray[i - left];

                    b += weight * source_buffer[i * from_step];
                    g += weight * source_buffer[i * from_step + 1];
                    r += weight * source_buffer[i * from_step + 2];
                }

                dest_buffer[ndx * to_step] = b;
                dest_buffer[ndx * to_step + 1] = g;
                dest_buffer[ndx * to_step + 2] = r;
            }
        }
        else{
            const uint32_t min_channels = MIN(from_step, to_step);
            float* avg = (float*)alloca(min_channels * sizeof(float));
            for (ndx = 0; ndx < dest_buffer_count; ndx++) {
                const int left = weights[ndx].Left;
                const int right = weights[ndx].Right;

                const float* __restrict weightArray = weights[ndx].Weights;

                /* Accumulate each channel */
                for (int i = left; i <= right; i++) {
                    const float weight = weightArray[i - left];

                    for (uint32_t j = 0; j < min_channels; j++)
                        avg[j] += weight * source_buffer[i * from_step + j];
                }

                for (uint32_t j = 0; j < min_channels; j++)
                    dest_buffer[ndx * to_step + j] = avg[j];
            }
        }
    }
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


