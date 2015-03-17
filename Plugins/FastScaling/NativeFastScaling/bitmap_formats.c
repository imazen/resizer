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

const int MAX_BPP = 4;

static bool are_valid_bitmap_dimensions(int sx, int sy)
{
    return sx > 0 && sy > 0 && sx * sy * MAX_BPP < INT_MAX;
}

uint32_t BitmapPixelFormat_bytes_per_pixel (BitmapPixelFormat format){
    return (uint32_t)format;
}


BitmapBgra * create_bitmap_bgra_header(Context * context, int sx, int sy){
    BitmapBgra * im;

    if (!are_valid_bitmap_dimensions(sx, sy)) {
	CONTEXT_SET_LAST_ERROR(context, Invalid_BitmapBgra_dimensions);
        return NULL;
    }

    im = (BitmapBgra *)ir_calloc(1,sizeof(BitmapBgra));
    if (im == NULL) {
	CONTEXT_SET_LAST_ERROR(context, Out_of_memory);
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


BitmapBgra * create_bitmap_bgra(Context * context, int sx, int sy, bool zeroed, BitmapPixelFormat format)
{
    BitmapBgra * im = create_bitmap_bgra_header(context, sx, sy);
    if (im == NULL) {
	return NULL;
    }
    im->fmt = format;
    im->stride = im->w * BitmapPixelFormat_bytes_per_pixel(im->fmt);
    im->pixels_readonly = false;
    im->stride_readonly = false;
    im->borrowed_pixels = false;
    im->alpha_meaningful = im->fmt == Bgra32;
    if (zeroed) {
        im->pixels = (unsigned char *)ir_calloc(im->h * im->stride, sizeof(unsigned char));
    }
    else {
        im->pixels = (unsigned char *)ir_malloc(im->h * im->stride);
    }
    if (im->pixels == NULL) {
        ir_free(im);
	CONTEXT_SET_LAST_ERROR(context, Out_of_memory);
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


BitmapFloat * create_bitmap_float_header(int sx, int sy, int channels){
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


BitmapFloat * create_bitmap_float(int sx, int sy, int channels, bool zeroed)
{
    BitmapFloat * im = create_bitmap_float_header(sx, sy, channels);
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


void destroy_bitmap_float(BitmapFloat * im)
{
    if (im == NULL) return;

    if (!im->pixels_borrowed) {
        ir_free(im->pixels);
    }
    im->pixels = NULL;
    ir_free(im);
}

