#include "Stdafx.h"
#include "shared.h"
#pragma once
#pragma unmanaged



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



static inline void
ScaleBgraFloat(float *source_buffer, unsigned int source_buffer_count, unsigned int source_buffer_len,
float *dest_buffer, unsigned int dest_buffer_count, unsigned int dest_buffer_len, ContributionType * weights, int from_step = 4, int to_step = 4){

    unsigned int ndx;

    // if both have alpha, process it
    if (from_step == 4 && to_step == 4)
    {
        for (ndx = 0; ndx < dest_buffer_count; ndx++) {
            float r = 0, g = 0, b = 0, a = 0;
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
                a += weight * source_buffer[i * from_step + 3];
            }

            dest_buffer[ndx * to_step] = b;
            dest_buffer[ndx * to_step + 1] = g;
            dest_buffer[ndx * to_step + 2] = r;
            dest_buffer[ndx * to_step + 3] = a;
        }
    }
    // otherwise do the same thing without 4th chan
    // (ifs in loops are expensive..)
    else
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

}


#ifdef ENABLE_INTERNAL_PREMULT
#define ALPHA_LUT_PREMULT(x) uchar_clamp_ff(lut[src_start[bix + x]] * lut[src_start[bix + 3]] / 255)
#else
#define ALPHA_LUT_PREMULT(x) src_start[bix + x]
#endif

#ifdef ENABLE_GAMMA_CORRECTION
#define RESTORE_GAMMA(x) linear_to_srgb(x)
#define GAMMA_LUT_OFFSET 256 +
#else
#define RESTORE_GAMMA(x) (x)
#define GAMMA_LUT_OFFSET
#endif

#ifdef ENABLE_COMPOSITING
#define COMPOSIT_ALPHA + lut[dst_start[3]] * float(1 - dest_buffers[dest_buffer_start + 3] / 255)
#define BLEND_ALPHA(ch, x) ((x + lut[dst_start[ch]] * lut[*(dst_start + 3)] / 255 * (1 - dest_buffers[dest_buffer_start + 3] / 255)) / out_alpha * 255)
#elif defined(ENABLE_INTERNAL_PREMULT)
#define COMPOSIT_ALPHA
#define BLEND_ALPHA(ch, x) (x * 255 / dest_buffers[dest_buffer_start + 3])
#else
#define COMPOSIT_ALPHA
#define BLEND_ALPHA(ch, x) (x)
#endif


static inline void ScaleXAndPivotRows(BitmapBgraPtr source_bitmap, unsigned int start_row, unsigned int row_count, ContributionType * weights, BitmapBgraPtr dest, float *source_buffers, unsigned int source_buffer_len, float *dest_buffers, unsigned int dest_buffer_len, float *lut){

    register unsigned int row, bix, bufferSet;
    const register unsigned int from_pixel_count = source_bitmap->w;
    const register unsigned int to_pixel_count = dest->h;

    for (row = 0; row < row_count; row++)
    {
        unsigned char *src_start = source_bitmap->pixels + (start_row + row)*source_bitmap->stride;

        if (source_bitmap->bpp == 3)
        {
            for (bix = 0; bix < source_buffer_len; bix++)
                source_buffers[row * source_buffer_len + bix] = lut[GAMMA_LUT_OFFSET src_start[bix]];
        }
        else
        {
            for (bix = 0; bix < source_buffer_len; bix += 4)
            {
                source_buffers[row * source_buffer_len + bix] = lut[GAMMA_LUT_OFFSET ALPHA_LUT_PREMULT(0)];
                source_buffers[row * source_buffer_len + bix + 1] = lut[GAMMA_LUT_OFFSET ALPHA_LUT_PREMULT(1)];
                source_buffers[row * source_buffer_len + bix + 2] = lut[GAMMA_LUT_OFFSET ALPHA_LUT_PREMULT(2)];
                source_buffers[row * source_buffer_len + bix + 3] = lut[src_start[bix + 3]];
            }
        }
    }

    //Actual scaling seems responsible for about 40% of execution time
    for (bufferSet = 0; bufferSet < row_count; bufferSet++){
        ScaleBgraFloat(source_buffers + (source_buffer_len * bufferSet), from_pixel_count, source_buffer_len,
            dest_buffers + (dest_buffer_len * bufferSet), to_pixel_count, dest_buffer_len, weights, source_bitmap->bpp, dest->bpp);
    }

    

    unsigned char *dst_start = dest->pixels + start_row * dest->bpp;
    int stride_offset = dest->stride - dest->bpp * row_count;
    float out_alpha;


    if (source_bitmap->bpp == 4)
    {
        if (dest->bpp == 4)
        {
            for (bix = 0; bix < to_pixel_count; bix++){
                int dest_buffer_start = bix * dest->bpp;
                for (bufferSet = 0; bufferSet < row_count; bufferSet++){
                    out_alpha = dest_buffers[dest_buffer_start + 3] COMPOSIT_ALPHA;
                    *dst_start = uchar_clamp_ff(BLEND_ALPHA(0, RESTORE_GAMMA(dest_buffers[dest_buffer_start])));
                    *(dst_start + 1) = uchar_clamp_ff(BLEND_ALPHA(1, RESTORE_GAMMA(dest_buffers[dest_buffer_start + 1])));
                    *(dst_start + 2) = uchar_clamp_ff(BLEND_ALPHA(2, RESTORE_GAMMA(dest_buffers[dest_buffer_start + 2])));
                    *(dst_start + 3) = uchar_clamp_ff(out_alpha);
                    dest_buffer_start += dest_buffer_len;
                    dst_start += dest->bpp;
                }
                dst_start += stride_offset;
            }
        }
        else
        {
            // shouldn't be possible?
        }
    }
    else
    {
        for (bix = 0; bix < to_pixel_count; bix++){
            int dest_buffer_start = bix * dest->bpp;

            for (bufferSet = 0; bufferSet < row_count; bufferSet++){
                *dst_start = uchar_clamp_ff(RESTORE_GAMMA(dest_buffers[dest_buffer_start]));
                *(dst_start + 1) = uchar_clamp_ff(RESTORE_GAMMA(dest_buffers[dest_buffer_start + 1]));
                *(dst_start + 2) = uchar_clamp_ff(RESTORE_GAMMA(dest_buffers[dest_buffer_start + 2]));
                dest_buffer_start += dest_buffer_len;
                dst_start += dest->bpp;
            }

            dst_start += stride_offset;
        }

        if (dest->bpp == 4)
        {
            dst_start = dest->pixels + start_row * dest->bpp;

            for (bix = 0; bix < to_pixel_count; bix++){
                for (bufferSet = 0; bufferSet < row_count; bufferSet++){
                    *(dst_start + 3) = 0xFF;
                    dst_start += dest->bpp;
                }
                dst_start += stride_offset;
            }
        }
    }
}


static inline int ScaleXAndPivot(const BitmapBgraPtr pSrc,
    const BitmapBgraPtr pDst, const InterpolationDetailsPtr details, float *lut)
{
    unsigned int line_ndx;
    LineContribType * contrib;

    contrib = ContributionsCalc(pDst->h, pSrc->w, details);
    if (contrib == NULL) {
        return 0;
    }

    int buffer = 4; //using buffer=5 seems about 6% better than most other non-zero values. 

    unsigned int source_buffer_len = pSrc->w * pSrc->bpp;
    float *sourceBuffers = (float *)malloc(sizeof(float) * source_buffer_len * buffer);

    unsigned int dest_buffer_len = pDst->h * pDst->bpp;
    float *destBuffers = (float *)malloc(sizeof(float) * dest_buffer_len * buffer);


    /* Scale each line */
    for (line_ndx = 0; line_ndx < pSrc->h; line_ndx += buffer) {

        ScaleXAndPivotRows(pSrc, line_ndx, MIN(pSrc->h - line_ndx, buffer), contrib->ContribRow, pDst,
            sourceBuffers, source_buffer_len, destBuffers, dest_buffer_len, lut);
    }

    free(sourceBuffers);
    free(destBuffers);

    ContributionsFree(contrib);

    return 1;
}


