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

#include "bitmap_formats.h"
#include "math_functions.h"
#include "ir_alloc.h"
#include "fastscaling.h"

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

void DestroyBitmapBgra(BitmapBgra * im)
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
    im->color = ColorSpace_RGBLinear_BGR;
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
