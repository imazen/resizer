/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */
#pragma once
#include "fastscaling_private"

#ifdef _MSC_VER
#pragma unmanaged
#endif

#ifdef __cplusplus
extern "C" {
#endif

static inline float
linear_to_srgb(float clr) {
    // Gamma correction
    // http://www.4p8.com/eric.brasseur/gamma.html#formulas

    if (clr <= 0.0031308f)
        return 12.92f * clr * 255.0f;

    // a = 0.055; ret ((1+a) * s**(1/2.4) - a) * 255
    return 1.055f  * 255.0f * ((float)pow(clr, 0.41666666f)) - 14.025f;
}


static inline float
srgb_to_linear(float s) {
    if (s <= 0.04045f)
       return s / 12.92f;
    else
        return (float)pow((s + 0.055f) / (1 + 0.055f), 2.4f);
}




static inline void linear_to_yxz(float * bgr){

    const float R = bgr[2];
    const float G = bgr[1];
    const float B = bgr[0];

    bgr[0] = 0.212671f*R + 0.71516f *G + 0.072169f*B; //Y
    bgr[1] = 0.412453f*R + 0.35758f *G + 0.180423f*B; //X
    bgr[2] = 0.019334f*R + 0.119193f*G + 0.950227f*B; //Z

}

static inline void linear_to_luv(float * bgr){
    //Observer= 2�, Illuminant= D65

    const float xn = 0.312713f;
    const float yn = 0.329016f;
    const float Yn = 1.0f;
    const float un = 4 * xn / (-2 * xn + 12 * yn + 3);
    const float vn = 9 * yn / (-2 * xn + 12 * yn + 3);
    const float y_split = 0.00885645f;
    const float y_adjust = 903.3f;

    const float R = bgr[2];
    const float G = bgr[1];
    const float B = bgr[0];

    if (R == 0 && G == 0 && B == 0){
        bgr[0] = 0;
        bgr[1] = bgr[2] = 100;
        return;
    }

    const float X = 0.412453f*R + 0.35758f *G + 0.180423f*B;
    const float Y = 0.212671f*R + 0.71516f *G + 0.072169f*B;
    const float Z = 0.019334f*R + 0.119193f*G + 0.950227f*B;



    const float Yd = Y / Yn;

    const float u = 4 * X / (X + 15 * Y + 3 * Z);
    const float v = 9 * Y / (X + 15 * Y + 3 * Z);
    const float L = bgr[0] /* L */ = Yd > y_split ? (116 * ((float)pow(Yd, (float)(1.0f / 3.0f))) - 16) : y_adjust * Yd;
    bgr[1]/* U */ = 13 * L*(u - un) + 100;
    bgr[2] /* V */ = 13 * L*(v - vn) + 100;
}

static inline void luv_to_linear(float * luv){
    //D65 white point :
    const float L = luv[0];
    const float U = luv[1] - 100.0f;
    const float V = luv[2] - 100.0f;
    if (L == 0){
        luv[0] = luv[1] = luv[2] = 0;
        return;
    }

    const float xn = 0.312713f;
    const float yn = 0.329016f;
    const float Yn = 1.0f;
    const float un = 4 * xn / (-2 * xn + 12 * yn + 3);
    const float vn = 9 * yn / (-2 * xn + 12 * yn + 3);
    const float y_adjust_2 = 0.00110705645f;

    const float u = U / (13 * L) + un;
    const float v = V / (13 * L) + vn;
    const float Y = L > 8 ? Yn * ((float)pow((L + 16) / 116, 3)) : Yn * L * y_adjust_2;
    const float X = (9 / 4.0f) * Y * u / v;// -9 * Y * u / ((u - 4) * v - u * v) = (9 / 4) * Y * u / v;
    const float Z = (9 * Y - 15 * v * Y - v * X) / (3 * v);


    const float r = 3.240479f*X - 1.53715f *Y - 0.498535f*Z;
    const float g = -0.969256f*X + 1.875991f*Y + 0.041556f*Z;
    const float b = 0.055648f*X - 0.204043f*Y + 1.057311f*Z;
    luv[0] = b; luv[1] = g; luv[2] = r;

}

static inline void yxz_to_linear(float * yxz){
    //D65 white point :
    const float Y = yxz[0];
    const float X = yxz[1];
    const float Z = yxz[2];

    yxz[2] = 3.240479f*X - 1.53715f *Y - 0.498535f*Z; //r
    yxz[1] = -0.969256f*X + 1.875991f*Y + 0.041556f*Z; //g
    yxz[0] = 0.055648f*X - 0.204043f*Y + 1.057311f*Z; //b

}

static int linear_to_luv_rows(BitmapFloat * bit, const uint32_t start_row, const  uint32_t row_count)
{
    if ((bit->w * bit->channels) != bit->float_stride)
    {
        return -1;
    }
     float * start_at = bit->float_stride * start_row  + bit->pixels;

    const float * end_at = bit->float_stride * (start_row + row_count) + bit->pixels;

    for (float* pix = start_at; pix < end_at; pix++){
        linear_to_luv(pix);
    }
    return 0;
}

static int luv_to_linear_rows(BitmapFloat * bit, const uint32_t start_row, const  uint32_t row_count)
{
    if ((bit->w * bit->channels) != bit->float_stride)
    {
        return -1;
    }
    float * start_at = bit->float_stride * start_row + bit->pixels;

    const float * end_at = bit->float_stride * (start_row + row_count) + bit->pixels;

    for (float* pix = start_at; pix < end_at; pix++){
        luv_to_linear(pix);
    }
    return 0;
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

#ifdef __cplusplus
}
#endif

