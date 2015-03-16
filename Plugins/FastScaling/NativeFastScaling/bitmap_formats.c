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

#include "fastscaling_private.h"
#include "bitmap_formats.h"
#include "math_functions.h"
#include "ir_alloc.h"


BitmapBgra * create_bitmap_bgra_header(int sx, int sy){
    BitmapBgra * im;

    if (overflow2(sx, sy) || overflow2(sizeof(int *), sy) || overflow2(sizeof(int), sx)) {
        return NULL;
    }

    im = (BitmapBgra *)ir_calloc(1,sizeof(BitmapBgra));
    if (im == NULL) {
        return NULL;
    }
    im->w = sx;
    im->h = sy;
    im->pixels = NULL;
    im->pixels_readonly = true;
    im->stride_readonly = true;
    im->borrowed_pixels = true;
    im->can_reuse_space = false;
    return im;
}


BitmapBgra * create_bitmap_bgra(int sx, int sy, bool zeroed, int bpp)
{
    BitmapBgra * im = create_bitmap_bgra_header(sx, sy);
    if (im == NULL) { 
	return NULL;
    }
    im->bpp = bpp;
    im->stride = im->w * bpp;
    im->pixels_readonly = false;
    im->stride_readonly = false;
    im->borrowed_pixels = false;
    im->alpha_meaningful = bpp == 4;
    if (zeroed) {
        im->pixels = (unsigned char *)ir_calloc(sy * im->stride, sizeof(unsigned char));
    }
    else {
        im->pixels = (unsigned char *)ir_malloc(sy * im->stride);
    }
    if (im->pixels == NULL) {
        ir_free(im);
        return NULL;
    }
    return im;
}

void destroy_bitmap_bgra(BitmapBgra * im)
{
    if (im == NULL) return;

    if (!im->borrowed_pixels) {
        ir_free(im->pixels);
    }
    ir_free(im);
}

BitmapFloat * CreateBitmapFloatHeader(int sx, int sy, int channels){
    BitmapFloat * im;

    if (overflow2(sx, sy) || overflow2(sizeof(int *), sy) || overflow2(sizeof(int), sx)) {
        return NULL;
    }

    im = (BitmapFloat *)ir_calloc(1,sizeof(BitmapFloat));
    if (!im) {
        return NULL;
    }
    im->w = sx;
    im->h = sy;
    im->pixels = NULL;
    im->pixels_borrowed = true;
    im->channels = channels;
    im->float_stride = sx * channels;
    im->float_count = im->float_stride * sy;
    im->alpha_meaningful = channels == 4;
    im->alpha_premultiplied = true;
    return im;
}


BitmapFloat * CreateBitmapFloat(int sx, int sy, int channels, bool zeroed)
{
    BitmapFloat * im = CreateBitmapFloatHeader(sx, sy, channels);
    if (im == NULL){ return NULL; }
    im->pixels_borrowed = false;
    if (zeroed){
        im->pixels = (float*)ir_calloc(im->float_count, sizeof(float));
    }
    else{
        im->pixels = (float *)ir_malloc(im->float_count * sizeof(float));
    }
    if (!im->pixels) {
        ir_free(im);
        return NULL;
    }
    return im;
}


void DestroyBitmapFloat(BitmapFloat * im)
{
    if (im == NULL) return;

    if (!im->pixels_borrowed) {
        ir_free(im->pixels);
    }
    im->pixels = NULL;
    ir_free(im);
}

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
