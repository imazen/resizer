#include "Stdafx.h"
#include "shared.h"
#include "sharpening.h"
#pragma once
#pragma unmanaged




static inline void
ScaleBgraFloatRow(BitmapFloatPtr from, const uint32_t from_row, BitmapFloatPtr to, const uint32_t to_row, ContributionType * weights){

    const uint32_t from_step = from->channels;
    const uint32_t to_step = to->channels;
    const uint32_t dest_buffer_count = to->w;
    const float* __restrict source_buffer = from->pixels + (from_row * from->float_stride);
    float* __restrict dest_buffer = to->pixels + (to_row * to->float_stride);

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
        float* avg = (float*)_malloca(min_channels * sizeof(float));
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
        _freea(avg);
    }

}






static inline int ScaleXAndPivotRows(BitmapBgraPtr source_bitmap, unsigned int start_row, unsigned int row_count, ContributionType * weights, BitmapBgraPtr dest, 
    BitmapFloatPtr src_buf, BitmapFloatPtr dst_buf, float *lut, float sharpen_percent, float* const kernel, int kernel_radius, float kernel_threshold, bool use_luv){


    if (convert_srgb_to_linear(source_bitmap, start_row, src_buf, 0, row_count, lut)){
        return -1;
    }


    const uint32_t from_pixel_count = source_bitmap->w;
    const uint32_t to_pixel_count = dest->h;


    //Actual scaling seems responsible for about 40% of execution time
    for (uint32_t row = 0; row < row_count; row++){

       
       
        ScaleBgraFloatRow(src_buf, row, dst_buf, row, weights);
        

        //float *dest_row_start = dst_buf->pixels + (dst_buf->float_stride * row);

        //if (use_luv){
        //    for (uint32_t bix = 0; bix < dst_buf->w; bix += dst_buf->channels)
        //    {
        //        linear_to_luv(dest_row_start + bix);
        //    }
        //}


        //if (kernel_radius > 0){
        //    if (ConvolveBgraFloatInPlace(dst_buf, kernel, kernel_radius, kernel_threshold, use_luv ? 1 : dst_buf->channels, row, 1)){
        //        return -3;
        //    }
        //}

        //if (sharpen_percent > 0){
        //    SharpenBgraFloatInPlace(dest_row_start, to_pixel_count, sharpen_percent, dest->bpp);
        //}

        //if (use_luv){
        //    for (uint32_t bix = 0; bix < dst_buf->w; bix += dst_buf->channels)
        //    {
        //        luv_to_linear(dest_row_start + bix);
        //    }
        //}
    }

    if (pivoting_composite_linear_over_srgb(dst_buf, 0, dest, start_row, row_count, lut)){
        return -2;
    }
    return 0;
}


static int ScaleXAndPivot(const BitmapBgraPtr pSrc,
    const BitmapBgraPtr pDst, const InterpolationDetailsPtr details, float *lut)
{

    int return_code = 0;
    uint32_t line_ndx;
    LineContribType * contrib;

    contrib = ContributionsCalc(pDst->h, pSrc->w, details);
    if (contrib == NULL) { return_code = -1; goto cleanup; }

    uint32_t buffer = 4; //using buffer=5 seems about 6% better than most other non-zero values. 

    uint32_t scaling_bpp = (pSrc->bpp == 4 && !pSrc->alpha_meaningful) ? 3 : pSrc->bpp;

    BitmapFloatPtr source_buf = CreateBitmapFloat(pSrc->w, buffer, scaling_bpp, false);
    if (source_buf == NULL)  { return_code = -1; goto cleanup; }
    BitmapFloatPtr dest_buf = CreateBitmapFloat(pDst->h, buffer, scaling_bpp, false);
    if (source_buf == NULL)  { return_code = -1; goto cleanup; }
    
    /* Scale each set of lines */
    for (line_ndx = 0; line_ndx < pSrc->h; line_ndx += buffer) {
        const uint32_t row_count = MIN(pSrc->h - line_ndx, buffer);

        int result = ScaleXAndPivotRows(pSrc, line_ndx, row_count, contrib->ContribRow, pDst,
            source_buf, dest_buf, lut,details->linear_sharpen ? (float)details->post_resize_sharpen_percent : 0,details->convolution_kernel,details->kernel_radius, details->kernel_threshold, details->use_luv);
        
        
        
        if (result){
            return_code = result;
            goto cleanup;
        }
    }

    
cleanup:
    if (contrib != NULL) ContributionsFree(contrib);
    if (source_buf != NULL) DestroyBitmapFloat(source_buf);
    if (dest_buf != NULL) DestroyBitmapFloat(dest_buf);

    return return_code;
}


