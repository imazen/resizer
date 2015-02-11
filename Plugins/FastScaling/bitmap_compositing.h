#pragma once
#pragma unmanaged




static void unpack24bitRow(uint32_t width, unsigned char* sourceLine, unsigned char* destArray){
    for (uint32_t i = 0; i < width; i++){

        memcpy(destArray + i * 4, sourceLine + i * 3, 3);
        destArray[i * 4 + 3] = 255;
    }
}

#define ro __restrict const 

#ifdef ENABLE_INTERNAL_PREMULT
#define premultiply_alpha(x) (uchar_clamp_ff(lut[src_start[bix + (x)]] * lut[src_start[bix + 3]] / 255.0f))
#define demultiply_alpha(x) ((x) * 255.0f / dest_buffers[dest_buffer_start + 3])
#else
#define premultiply_alpha(x) (src_start[bix + x])
#define demultiply_alpha(x) (x)
#endif

#ifdef ENABLE_COMPOSITING
#define composit_alpha + lut[dst_start[3]] * (1 - dest_buffers[dest_buffer_start + 3] / 255.0f)
#define blend_alpha(ch, x) (((x) + lut[dst_start[ch]] * lut[dst_start[3]] / 255.0f * (1 - dest_buffers[dest_buffer_start + 3] / 255.0f)) / out_alpha * 255.0f)
#define blend_matte(ch, x) ((x) + lut[dest->matte_color[ch]] * (1 - dest_buffers[dest_buffer_start + 3] / 255.0f))
#elif defined(ENABLE_INTERNAL_PREMULT)
#define composit_alpha
#define blend_alpha(ch, x) ((x) * 255.0f / dest_buffers[dest_buffer_start + 3])
#define blend_matte(ch, x) ((x) * 255.0f / dest_buffers[dest_buffer_start + 3])
#else
#define composit_alpha
#define blend_alpha(ch, x) (x)
#define blend_matte(ch, x) (x)
#endif

#define srgb_to_linear(x) (lut[256 + (x)])



static int convert_srgb_to_linear(BitmapBgraPtr src, const uint32_t from_row, BitmapFloatPtr dest, const uint32_t dest_row, const uint32_t row_count, float* const __restrict lut){

    if (src->w != dest->w || src->bpp < dest->channels) return -1;

    const uint32_t w = src->w;
    const uint32_t units = w * src->bpp;
    const uint32_t from_step = src->bpp;
    const uint32_t to_step = src->bpp;
    const uint32_t copy_step = MIN(from_step, to_step);

    for (uint32_t row = 0; row < row_count; row++)
    {
        const  uint8_t*   src_start = src->pixels + (from_row + row)*src->stride;
        float* buf = dest->pixels + (dest->float_stride * (row + dest_row));
        if (copy_step == 3)
        {
            for (uint32_t to_x = 0, bix = 0; bix < units; to_x += to_step, bix += from_step){
                buf[to_x] = srgb_to_linear(src_start[bix]);
                buf[to_x + 1] = srgb_to_linear(src_start[bix + 1]);
                buf[to_x + 2] = srgb_to_linear(src_start[bix + 2]);
            }
        }
        else if (copy_step == 4)
        {
            for (uint32_t to_x = 0, bix = 0; bix < units; to_x += to_step, bix += from_step){
                {
                    buf[to_x] = srgb_to_linear(premultiply_alpha(0));
                    buf[to_x + 1] = srgb_to_linear(premultiply_alpha(1));
                    buf[to_x + 2] = srgb_to_linear(premultiply_alpha(2));
                    buf[to_x + 3] = lut[src_start[bix + 3]];
                }
            }
        }
        return 0;
    }
}


static int pivoting_composite_linear_over_srgb(BitmapFloatPtr src, const uint32_t from_row, BitmapBgraPtr dest, const uint32_t dest_row, const uint32_t row_count, float* const __restrict lut){
    if (src->w != dest->h) return -1;

    float* const __restrict dest_buffers = src->pixels;
    const uint32_t dest_buffer_len = src->float_stride;


    uint8_t* dst_start = dest->pixels + dest_row * dest->bpp;
    uint32_t stride_offset = dest->stride - dest->bpp * row_count;
    float out_alpha;

    uint32_t bix, bufferSet;
    const uint32_t w = src->w;


    if (src->channels == 4 && src->alpha_meaningful && dest->bpp == 4)
    {
        if (dest->compositing_mode == BitmapCompositingMode::Blend_with_self)
        {
            for (bix = 0; bix < w; bix++){
                uint32_t dest_buffer_start = bix * src->channels;
                for (bufferSet = 0; bufferSet < row_count; bufferSet++){
                    out_alpha = dest_buffers[dest_buffer_start + 3] composit_alpha;
                    dst_start[0] = uchar_clamp_ff(blend_alpha(0, linear_to_srgb(dest_buffers[dest_buffer_start + 0])));
                    dst_start[1] = uchar_clamp_ff(blend_alpha(1, linear_to_srgb(dest_buffers[dest_buffer_start + 1])));
                    dst_start[2] = uchar_clamp_ff(blend_alpha(2, linear_to_srgb(dest_buffers[dest_buffer_start + 2])));
                    dst_start[3] = uchar_clamp_ff(out_alpha);
                    dest_buffer_start += dest_buffer_len;
                    dst_start += dest->bpp;
                }
                dst_start += stride_offset;
            }
        }
        else if (dest->compositing_mode == BitmapCompositingMode::Blend_with_matte)
        {
            for (bix = 0; bix < w; bix++){
                uint32_t dest_buffer_start = bix * src->channels;
                for (bufferSet = 0; bufferSet < row_count; bufferSet++){
                    dst_start[0] = uchar_clamp_ff(blend_matte(0, linear_to_srgb(dest_buffers[dest_buffer_start + 0])));
                    dst_start[1] = uchar_clamp_ff(blend_matte(1, linear_to_srgb(dest_buffers[dest_buffer_start + 1])));
                    dst_start[2] = uchar_clamp_ff(blend_matte(2, linear_to_srgb(dest_buffers[dest_buffer_start + 2])));
                    dst_start[3] = 255;
                    dest_buffer_start += dest_buffer_len;
                    dst_start += dest->bpp;
                }
                dst_start += stride_offset;
            }
        }
        else if (dest->compositing_mode == BitmapCompositingMode::Replace_self)
        {
            for (bix = 0; bix < w; bix++){
                uint32_t dest_buffer_start = bix * src->channels;
                for (bufferSet = 0; bufferSet < row_count; bufferSet++){
                    out_alpha = dest_buffers[dest_buffer_start + 3];
                    dst_start[0] = uchar_clamp_ff(demultiply_alpha(linear_to_srgb(dest_buffers[dest_buffer_start + 0])));
                    dst_start[1] = uchar_clamp_ff(demultiply_alpha(linear_to_srgb(dest_buffers[dest_buffer_start + 1])));
                    dst_start[2] = uchar_clamp_ff(demultiply_alpha(linear_to_srgb(dest_buffers[dest_buffer_start + 2])));
                    dst_start[3] = uchar_clamp_ff(dest_buffers[dest_buffer_start + 3]);
                    dest_buffer_start += dest_buffer_len;
                    dst_start += dest->bpp;
                }
                dst_start += stride_offset;
            }
        }
    }
    // shouldn't be possible?


    else
    {
        for (bix = 0; bix < w; bix++){
            uint32_t dest_buffer_start = bix * src->channels;

            for (bufferSet = 0; bufferSet < row_count; bufferSet++){
                *dst_start = uchar_clamp_ff(linear_to_srgb(dest_buffers[dest_buffer_start]));
                *(dst_start + 1) = uchar_clamp_ff(linear_to_srgb(dest_buffers[dest_buffer_start + 1]));
                *(dst_start + 2) = uchar_clamp_ff(linear_to_srgb(dest_buffers[dest_buffer_start + 2]));
                dest_buffer_start += dest_buffer_len;
                dst_start += dest->bpp;
            }

            dst_start += stride_offset;
        }

        if (dest->bpp == 4)
        {
            dst_start = dest->pixels + dest_row * dest->bpp;

            for (bix = 0; bix < w; bix++){
                for (bufferSet = 0; bufferSet < row_count; bufferSet++){
                    *(dst_start + 3) = 0xFF;
                    dst_start += dest->bpp;
                }
                dst_start += stride_offset;
            }
        }
    }
    return 0;
}





#undef ro