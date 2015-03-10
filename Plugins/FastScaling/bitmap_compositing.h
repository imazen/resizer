#pragma once
#pragma unmanaged
#include "Stdafx.h"
#include "shared.h"




static int convert_srgb_to_linear(BitmapBgraPtr src, const uint32_t from_row, BitmapFloatPtr dest, const uint32_t dest_row, const uint32_t row_count){

    if (src->w != dest->w || src->bpp < dest->channels) return -1;

    const LookupTables*  t = GetLookupTables();

    const uint32_t w = src->w;
    const uint32_t units = w * src->bpp;
    const uint32_t from_step = src->bpp;
    const uint32_t to_step = dest->channels;
    const uint32_t copy_step = MIN(from_step, to_step);

    for (uint32_t row = 0; row < row_count; row++)
    {
        //const   uint8_t*  __restrict  src_start = src->pixels + (from_row + row)*src->stride;
           uint8_t*    src_start = src->pixels + (from_row + row)*src->stride;
        
        float* buf = dest->pixels + (dest->float_stride * (row + dest_row));
        if (copy_step == 3)
        {
            for (uint32_t to_x = 0, bix = 0; bix < units; to_x += to_step, bix += from_step){
                buf[to_x] =     t->srgb_to_linear[src_start[bix]];
                buf[to_x + 1] = t->srgb_to_linear[src_start[bix + 1]];
                buf[to_x + 2] = t->srgb_to_linear[src_start[bix + 2]];
            }
            dest->alpha_premultiplied = false;
        }
        else if (copy_step == 4)
        {
            for (uint32_t to_x = 0, bix = 0; bix < units; to_x += to_step, bix += from_step){
                {
                    const float alpha = t->linear[src_start[bix + 3]];
                    buf[to_x] =     alpha * t->srgb_to_linear[src_start[bix]];
                    buf[to_x + 1] = alpha * t->srgb_to_linear[src_start[bix + 1]];
                    buf[to_x + 2] = alpha * t->srgb_to_linear[src_start[bix + 2]];
                    buf[to_x + 3] = alpha; 
                }
            }
            dest->alpha_premultiplied = true;
        }
        else{
            return -1;
        }
        
    }
    return 0;
}


static void unpack24bitRow(uint32_t width, unsigned char* sourceLine, unsigned char* destArray){
    for (uint32_t i = 0; i < width; i++){

        memcpy(destArray + i * 4, sourceLine + i * 3, 3);
        destArray[i * 4 + 3] = 255;
    }
}


static int vertical_flip_bgra(BitmapBgraPtr b){
    void* swap = ir_malloc(b->stride);
    if (swap == NULL){
        return -1;
    }
    for (uint32_t i = 0; i < b->h / 2; i++){
        void* top = b->pixels + (i * b->stride);
        void* bottom = b->pixels + ((b->h - 1 - i) * b->stride);
        memcpy(swap, top, b->stride);
        memcpy(top, bottom, b->stride);
        memcpy(bottom, swap, b->stride);
    }
    ir_free(swap);
    return 0;
}
static int  copy_bitmap_bgra(BitmapBgraPtr src, BitmapBgraPtr dst)
{
    // TODO: check sizes / overflows
    if (dst->w != src->w || dst->h != src->h) return -1;

    if (src->bpp == dst->bpp)
    {
        // recalculate line width as it can be different from the stride
        for (uint32_t y = 0; y < src->h; y++)
            memcpy(dst->pixels + y*dst->stride, src->pixels + y*src->stride, src->w*src->bpp);
    }
    else if (src->bpp == 3 && dst->bpp == 4)
    {
        for (uint32_t y = 0; y < src->h; y++)
            unpack24bitRow(src->w, src->pixels + y*src->stride, dst->pixels + y*dst->stride);
    }
    else{
        return -2;
    }
    return 0;
}

static int blend_matte(BitmapFloatPtr src, const uint32_t from_row, const uint32_t row_count, const uint8_t* const matte){
    //TODO, implement

    //Ensure alpha is demultiplied
    return 0;
}

static void demultiply_alpha(BitmapFloatPtr src, const uint32_t from_row, const uint32_t row_count){
    //TODO, implement
    for (uint32_t row = from_row; row < from_row + row_count; row++){
        uint32_t start_ix = row * src->float_stride;
        uint32_t end_ix = start_ix + src->w * src->channels;

        for (uint32_t ix = start_ix; ix < end_ix; ix += 4){
            const float alpha = src->pixels[ix + 3];
            if (alpha > 0){
                src->pixels[ix] /= alpha;
                src->pixels[ix + 1] /= alpha;
                src->pixels[ix + 2] /= alpha;
            }
        }
    }
}


static void copy_linear_over_srgb(BitmapFloatPtr src, const uint32_t from_row, BitmapBgraPtr dest, const uint32_t dest_row, const uint32_t row_count, const bool transpose){

     LookupTables*   t = GetLookupTables();


    const uint32_t dest_row_stride = transpose ? dest->bpp : dest->stride;
    const uint32_t dest_pixel_stride = transpose ? dest->stride : dest->bpp;
    const uint32_t srcitems = src->w *src->channels;
    const uint32_t ch = src->channels;
    const bool copy_alpha = dest->bpp == 4 && src->channels == 4 && src->alpha_meaningful;
    const bool clean_alpha = !copy_alpha && dest->bpp == 4;

    for (uint32_t row = 0; row < row_count; row++){
        //const float * const __restrict src_row = src->pixels + (row + from_row) * src->float_stride;
        float * src_row = src->pixels + (row + from_row) * src->float_stride;
        
        uint8_t * dest_row_bytes = dest->pixels + (dest_row + row) * dest_row_stride;

        for (uint32_t ix = 0; ix < srcitems; ix += ch){
            dest_row_bytes[0] = t->linear_to_srgb[clamp_01_to_04096(src_row[ix])];
            dest_row_bytes[1] = t->linear_to_srgb[clamp_01_to_04096(src_row[ix + 1])];
            dest_row_bytes[2] = t->linear_to_srgb[clamp_01_to_04096(src_row[ix + 2])];
            if (copy_alpha){
                dest_row_bytes[3] = uchar_clamp_ff(src_row[ix + 3] * 255);

            }
            if (clean_alpha){
                dest_row_bytes[3] = 0xff;
            }
            dest_row_bytes += dest_pixel_stride;
        }
    }

}


static int pivoting_composite_linear_over_srgb(BitmapFloatPtr src, const uint32_t from_row, BitmapBgraPtr dest, const uint32_t dest_row, const uint32_t row_count, const bool transpose){
    if (transpose ? src->w != dest->h : src->w != dest->w) return -1; //Add more bounds checks

    
    if (src->alpha_meaningful && src->channels == 4 && dest->compositing_mode == BitmapCompositingMode::Blend_with_matte){
        if (blend_matte(src, from_row, row_count, dest->matte_color)){
            return -9;
        }
    }
    if (src->channels == 4 && src->alpha_premultiplied && dest->compositing_mode != BitmapCompositingMode::Blend_with_self){
        //Demultiply
        demultiply_alpha(src, from_row, row_count);
    }
    
    bool can_compose = dest->compositing_mode == BitmapCompositingMode::Blend_with_self && src->alpha_meaningful && src->channels == 4;
    
    if (can_compose){
        //TODO handle composition
    }
    else{
        copy_linear_over_srgb(src, from_row, dest, dest_row, row_count, transpose);
    }
    //
    //const LookupTables* __restrict t = GetLookupTables();


    //float* const __restrict dest_buffers = src->pixels;
    //const uint32_t dest_buffer_len = src->float_stride;

    //const uint32_t dst_row_offset = transpose ? dest->bpp : dest->stride;
    //uint8_t *dst_start = dest->pixels + dest_row * dst_row_offset;
    //const uint32_t stride_offset = transpose ? (dest->stride - dest->bpp * row_count) :33333333333333333;
    //
    //float out_alpha;

    //uint32_t bix, bufferSet;
    //const uint32_t w = src->w;
   


    //if (src->channels == 4 && src->alpha_meaningful && dest->bpp == 4)
    //{
    //    if (dest->compositing_mode == BitmapCompositingMode::Blend_with_self)
    //    {
    //        for (bix = 0; bix < w; bix++){
    //            uint32_t dest_buffer_start = bix * src->channels;
    //            for (bufferSet = 0; bufferSet < row_count; bufferSet++){
    //                out_alpha = dest_buffers[dest_buffer_start + 3] composit_alpha;
    //                dst_start[0] = uchar_clamp_ff(blend_alpha(0, linear_to_srgb(dest_buffers[dest_buffer_start + 0])));
    //                dst_start[1] = uchar_clamp_ff(blend_alpha(1, linear_to_srgb(dest_buffers[dest_buffer_start + 1])));
    //                dst_start[2] = uchar_clamp_ff(blend_alpha(2, linear_to_srgb(dest_buffers[dest_buffer_start + 2])));
    //                dst_start[3] = uchar_clamp_ff(out_alpha);
    //                dest_buffer_start += dest_buffer_len;
    //                dst_start += dest->bpp;
    //            }
    //            dst_start += stride_offset;
    //        }
    //    }
    //    else if (dest->compositing_mode == BitmapCompositingMode::Blend_with_matte)
    //    {
    //        for (bix = 0; bix < w; bix++){
    //            uint32_t dest_buffer_start = bix * src->channels;
    //            for (bufferSet = 0; bufferSet < row_count; bufferSet++){
    //                dst_start[0] = uchar_clamp_ff(blend_matte(0, linear_to_srgb(dest_buffers[dest_buffer_start + 0])));
    //                dst_start[1] = uchar_clamp_ff(blend_matte(1, linear_to_srgb(dest_buffers[dest_buffer_start + 1])));
    //                dst_start[2] = uchar_clamp_ff(blend_matte(2, linear_to_srgb(dest_buffers[dest_buffer_start + 2])));
    //                dst_start[3] = 255;
    //                dest_buffer_start += dest_buffer_len;
    //                dst_start += dest->bpp;
    //            }
    //            dst_start += stride_offset;
    //        }
    //    }
    //    else if (dest->compositing_mode == BitmapCompositingMode::Replace_self)
    //    {
    //        for (bix = 0; bix < w; bix++){
    //            uint32_t dest_buffer_start = bix * src->channels;
    //            for (bufferSet = 0; bufferSet < row_count; bufferSet++){
    //                out_alpha = dest_buffers[dest_buffer_start + 3];
    //                dst_start[0] = uchar_clamp_ff(demultiply_alpha(linear_to_srgb(dest_buffers[dest_buffer_start + 0])));
    //                dst_start[1] = uchar_clamp_ff(demultiply_alpha(linear_to_srgb(dest_buffers[dest_buffer_start + 1])));
    //                dst_start[2] = uchar_clamp_ff(demultiply_alpha(linear_to_srgb(dest_buffers[dest_buffer_start + 2])));
    //                dst_start[3] = uchar_clamp_ff(dest_buffers[dest_buffer_start + 3]);
    //                dest_buffer_start += dest_buffer_len;
    //                dst_start += dest->bpp;
    //            }
    //            dst_start += stride_offset;
    //        }
    //    }
    //}
    //// shouldn't be possible?


    //else
    //{
    //    for (bix = 0; bix < w; bix++){
    //        uint32_t dest_buffer_start = bix * src->channels;

    //        for (bufferSet = 0; bufferSet < row_count; bufferSet++){
    //            *dst_start =       t->linear_to_srgb[clamp_01_to_01024(dest_buffers[dest_buffer_start])];
    //            *(dst_start + 1) = t->linear_to_srgb[clamp_01_to_01024(dest_buffers[dest_buffer_start + 1])];
    //            *(dst_start + 2) = t->linear_to_srgb[clamp_01_to_01024(dest_buffers[dest_buffer_start + 2])];
    //            dest_buffer_start += dest_buffer_len;
    //            dst_start += dest->bpp;
    //        }

    //        dst_start += stride_offset;
    //    }

    //    if (dest->bpp == 4)
    //    {
    //        dst_start = dest->pixels + dest_row * dest->bpp;

    //        for (bix = 0; bix < w; bix++){
    //            for (bufferSet = 0; bufferSet < row_count; bufferSet++){
    //                *(dst_start + 3) = 0xFF;
    //                dst_start += dest->bpp;
    //            }
    //            dst_start += stride_offset;
    //        }
    //    }
    //}
    return 0;
}


//#ifdef ENABLE_INTERNAL_PREMULT
//#define premultiply_alpha(x) (uchar_clamp_ff(t->linear[src_start[bix + (x)]] * t->linear[src_start[bix + 3]] / 255.0f))
//#define demultiply_alpha(x) ((x) * 255.0f / dest_buffers[dest_buffer_start + 3])
//#else
//#define premultiply_alpha(x) (src_start[bix + x])
//#define demultiply_alpha(x) (x)
//#endif
//
//#ifdef ENABLE_COMPOSITING
//#define composit_alpha + lut[dst_start[3]] * (1 - dest_buffers[dest_buffer_start + 3] / 255.0f)
//#define blend_alpha(ch, x) (((x) + lut[dst_start[ch]] * lut[dst_start[3]] / 255.0f * (1 - dest_buffers[dest_buffer_start + 3] / 255.0f)) / out_alpha * 255.0f)
//#define blend_matte(ch, x) ((x) + lut[dest->matte_color[ch]] * (1 - dest_buffers[dest_buffer_start + 3] / 255.0f))
//#elif defined(ENABLE_INTERNAL_PREMULT)
//#define composit_alpha
//#define blend_alpha(ch, x) ((x) * 255.0f / dest_buffers[dest_buffer_start + 3])
//#define blend_matte(ch, x) ((x) * 255.0f / dest_buffers[dest_buffer_start + 3])
//#else
//#define composit_alpha
//#define blend_alpha(ch, x) (x)
//#define blend_matte(ch, x) (x)
//#endif
//
//#define srgb_to_linear(x) (t->srgb_to_linear[x])
//
//
//
//#undef ro