#include "Stdafx.h"
#include "shared.h"
#pragma once
#pragma unmanaged

void InternalApplyMatrix(BitmapBgraPtr b, float *m[5])
{
    if (b->bpp == 4)
    {
        for (int x = 0; x < b->w; x++)
        for (int y = 0; y < b->h; y++)
        {
            unsigned char *data = b->pixels + b->stride * y + x * b->bpp;

            data[2] = uchar_clamp_ff(m[0][0] * data[2] + m[1][0] * data[1] + m[2][0] * data[0] + m[3][0] * data[3] + m[4][0]);
            data[1] = uchar_clamp_ff(m[0][1] * data[2] + m[1][1] * data[1] + m[2][1] * data[0] + m[3][1] * data[3] + m[4][1]);
            data[0] = uchar_clamp_ff(m[0][2] * data[2] + m[1][2] * data[1] + m[2][2] * data[0] + m[3][2] * data[3] + m[4][2]);
            data[3] = uchar_clamp_ff(m[0][3] * data[2] + m[1][3] * data[1] + m[2][3] * data[0] + m[3][3] * data[3] + m[4][3]);
        }
    }
    else if (b->bpp == 3)
    {
        for (int x = 0; x < b->w; x++)
        for (int y = 0; y < b->h; y++)
        {
            unsigned char *data = b->pixels + b->stride * y + x * b->bpp;

            data[2] = uchar_clamp_ff(m[0][0] * data[2] + m[1][0] * data[1] + m[2][0] * data[0] + m[4][0]);
            data[1] = uchar_clamp_ff(m[0][1] * data[2] + m[1][1] * data[1] + m[2][1] * data[0] + m[4][1]);
            data[0] = uchar_clamp_ff(m[0][2] * data[2] + m[1][2] * data[1] + m[2][2] * data[0] + m[4][2]);
        }
    }
}
