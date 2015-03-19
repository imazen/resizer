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

bool BitmapFloat_linear_to_luv_rows(Context * context, BitmapFloat * bit, const uint32_t start_row, const  uint32_t row_count)
{
    //TODO: Ensure that start_row + row_count is not > bit->h
    if ((bit->w * bit->channels) != bit->float_stride)
    {
        CONTEXT_error(context, Invalid_internal_state);
        return false;
    }
     float * start_at = bit->float_stride * start_row  + bit->pixels;

    const float * end_at = bit->float_stride * (start_row + row_count) + bit->pixels;

    for (float* pix = start_at; pix < end_at; pix++){
        linear_to_luv(pix);
    }
    return true;
}

bool BitmapFloat_luv_to_linear_rows(Context * context, BitmapFloat * bit, const uint32_t start_row, const  uint32_t row_count)
{
    //TODO: Ensure that start_row + row_count is not > bit->h
    if ((bit->w * bit->channels) != bit->float_stride)
    {
        CONTEXT_error(context, Invalid_internal_state);
        return false;
    }
    float * start_at = bit->float_stride * start_row + bit->pixels;

    const float * end_at = bit->float_stride * (start_row + row_count) + bit->pixels;

    for (float* pix = start_at; pix < end_at; pix++){
        luv_to_linear(pix);
    }
    return true;
}


static LookupTables * table = NULL;

void free_lookup_tables() {
    LookupTables * temp =  table;
    table = NULL;
    free(temp);
}

LookupTables * get_lookup_tables(void) {
    if (table == NULL){
        LookupTables * temp = (LookupTables*)malloc(sizeof(LookupTables));
        if (temp == NULL) return NULL;
        // Gamma correction
        // http://www.4p8.com/eric.brasseur/gamma.html#formulas

        // Store gamma adjusted in 256-511, linear in 0-255

        float *lin = temp->linear;
        float *to_lin = temp->srgb_to_linear;

        for (uint32_t n = 0; n < 256; n++)
        {
            float s = ((float)n) / 255.0f;
            lin[n] = s;
            to_lin[n] = srgb_to_linear(s);
        }

        if (table == NULL){
            //A race condition could cause a 3KB, one-time memory leak between these two lines.
            //we're OK with that. Better than locking during an inner loop
            table = temp;
        }
        else{
            free(temp);
        }
    }
    return table;
}


bool BitmapBgra_apply_color_matrix(Context * context, BitmapBgra * bmp, const uint32_t row, const uint32_t count, float* const __restrict  m[5])
{
    const uint32_t stride = bmp->stride;
    const uint32_t ch = BitmapPixelFormat_bytes_per_pixel(bmp->fmt);
    const uint32_t w = bmp->w;
    const uint32_t h = umin(row + count, bmp->h);
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
    }else{
        CONTEXT_error(context, Invalid_internal_state);
        return false;
    }
    return true;
}


bool BitmapFloat_apply_color_matrix(Context * context, BitmapFloat * bmp, const uint32_t row, const uint32_t count, float*  m[5])
{
    const uint32_t stride = bmp->float_stride;
    const uint32_t ch = bmp->channels;
    const uint32_t w = bmp->w;
    const uint32_t h = umin(row + count,bmp->h);
    switch (ch) {
    case 4: 
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
        return true;
    }
    case 3:
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
        return true;
    }
    default: {
        CONTEXT_error(context, Invalid_internal_state);
        return false;
    }
    }
}
