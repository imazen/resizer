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
ConvolveBgraFloatInPlace(float *source_buffer, const unsigned int source_buffer_count, unsigned int source_buffer_len, const float *kernel, const int radius,
const int step = 4){

    if (source_buffer_count < radius + 1) return; //Do nothing unless the image is at least half as wide as the kernel.
    unsigned int ndx;
    const int buffer_count = radius + 1;
    float*  buffer = (float *)malloc(sizeof(float) * buffer_count * step);
    float * avg = (float *)malloc(sizeof(float) * step);
    int circular_idx = 0;


    for (ndx = 0; ndx < source_buffer_count + buffer_count; ndx++) {
        //Flush old value
        if (ndx >= buffer_count){
            for (int j = 0; j < step; j++)
                source_buffer[(ndx - buffer_count) * step + j] = buffer[circular_idx * step + j];
        }
        //Calculate and enqueue new value
        if (ndx < source_buffer_count){
            const int left = ndx - radius;
            const int right = ndx + radius;
            int i;

            memset(avg, 0, sizeof(float) * step);

            if (left < 0 || right >= source_buffer_count){
                /* Accumulate each channel */
                for (i = left; i <= right; i++) {
                    const float weight = kernel[i - left];
                    //const int ix = i < 0 ? i * -1 : i >= source_buffer_count ? (source_buffer_count - i + source_buffer_count - 2) : i;
                    const int ix = i < 0 ? 0 : i >= source_buffer_count ? source_buffer_count - 1 : i;
                    for (int j = 0; j < step; j++)
                        avg[j] += weight * source_buffer[ix * step + j];
                }
            }
            else{
                /* Accumulate each channel */
                for (i = left; i <= right; i++) {
                    const float weight = kernel[i - left];
                    for (int j = 0; j < step; j++)
                        avg[j] += weight * source_buffer[i * step + j];
                }
            }

            //Enqueue new value
            for (int j = 0; j < step; j++)
                buffer[circular_idx * step + j] = avg[j];
        }
        circular_idx = (circular_idx + 1) % buffer_count;

    }
    free(avg);
    free(buffer);
}


static void
ConvolveBgraFloatDoubleBuffer(float *source_buffer, const unsigned int source_buffer_count, unsigned int source_buffer_len, const float *kernel, const int radius,
const int step = 4){

    float *buf = (float*)malloc(sizeof(float) * source_buffer_len);
    memcpy(buf, source_buffer, sizeof(float) * source_buffer_len);


    unsigned int ndx;
    float * avg = (float *)malloc(sizeof(float) * step);


    for (ndx = 0; ndx < source_buffer_count; ndx++) {

            const int left = ndx - radius;
            const int right = ndx + radius;
            int i;

            memset(avg, 0, sizeof(float) * step);

            if (left < 0 || right >= source_buffer_count){
                /* Accumulate each channel */
                for (i = left; i <= right; i++) {
                    const float weight = kernel[i - left];
                    const int ix = i < 0 ? i * -1 : i >= source_buffer_count ? (source_buffer_count - i + source_buffer_count - 2) : i;
                    for (int j = 0; j < step; j++)
                        avg[j] += weight * buf[ix * step + j];
                }
            }
            else{
                /* Accumulate each channel */
                for (i = left; i <= right; i++) {
                    const float weight = kernel[i - left];
                    for (int j = 0; j < step; j++)
                        avg[j] += weight * buf[i * step + j];
                }
            }



            //Store values
            for (int j = 0; j < step; j++)
                source_buffer[ndx * step + j] = avg[j];
       
    }
    free(buf);

}

