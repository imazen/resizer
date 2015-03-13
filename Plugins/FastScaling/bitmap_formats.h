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

enum BitmapPixelFormat {
    None = 0,
    Bgr24 = 24,
    Bgra32 = 32,
    Gray8 = 8
};
enum BitmapCompositingMode{
    Replace_self = 0,
    Blend_with_self = 1,
    Blend_with_matte = 2
};

enum ColorSpace{
    ColorSpace_None = 0,
    ColorSpace_sRGB_BGR = 1,
    ColorSpace_RGBLinear_BGR = 2,
    ColorSpace_LUV = 3,
    ColorSpace_XYZ_YXZ = 4,
    ColorSpace_Sigmoid = 5
};

typedef struct BitmapBgraStruct *BitmapBgraPtr;

//non-indexed bitmap
typedef struct BitmapBgraStruct{

    //bitmap width in pixels
    uint32_t w;
    //bitmap height in pixels
    uint32_t h;
    //byte length of each row (may include any amount of padding)
    uint32_t stride;
    //pointer to pixel 0,0; should be of length > h * stride
    unsigned char *pixels;
    //If true, we don't dispose of *pixels when we dispose the struct
    bool borrowed_pixels;
    //If false, we can even ignore the alpha channel on 4bpp
    bool alpha_meaningful;
    //If false, we can edit pixels without affecting the stride
    bool pixels_readonly;
    //If false, we can change the stride of the image.
    bool stride_readonly;

    //If true, we can reuse the allocated memory for other purposes. 
    bool can_reuse_space; 
    //TODO: rename to bytes_pp
    uint32_t bpp;

    BitmapPixelFormat pixel_format;

    //When using compositing mode blend_with_matte, this color will be used
    unsigned char *matte_color;
    ///If true, we don't dispose of *pixels when we dispose the struct
    bool borrowed_matte_color;

    BitmapCompositingMode compositing_mode;

} BitmapBgra;

typedef struct BitmapFloatStruct *BitmapFloatPtr;

typedef struct BitmapFloatStruct{
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



static BitmapBgraPtr CreateBitmapBgraHeader(int sx, int sy){
    BitmapBgraPtr im;

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


static BitmapBgraPtr CreateBitmapBgra(int sx, int sy, bool zeroed, int bpp)
{
    BitmapBgraPtr im = CreateBitmapBgraHeader(sx, sy);
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


inline void DestroyBitmapBgra(BitmapBgraPtr im)
{
    if (im == NULL) return;

    if (!im->borrowed_pixels) {
        ir_free(im->pixels);
    }
    ir_free(im);
}



static BitmapFloatPtr CreateBitmapFloatHeader(int sx, int sy, int channels){
    BitmapFloatPtr im;

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
    im->color = ColorSpace::ColorSpace_RGBLinear_BGR;
    im->alpha_meaningful = channels == 4;
    im->alpha_premultiplied = true;
    return im;
}


static BitmapFloatPtr CreateBitmapFloat(int sx, int sy, int channels, bool zeroed)
{
    BitmapFloatPtr im = CreateBitmapFloatHeader(sx, sy, channels);
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


static void DestroyBitmapFloat(BitmapFloatPtr im)
{
    if (im == NULL) return;

    if (!im->pixels_borrowed) {
        ir_free(im->pixels);
    }
    im->pixels = NULL;
    ir_free(im);
}
