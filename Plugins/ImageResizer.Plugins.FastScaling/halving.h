#include "Stdafx.h"
#include "shared.h"
#pragma once
#pragma unmanaged



static inline void HalveRowByDivisor(const unsigned char* from, unsigned short * to, const unsigned int to_count, const int divisor, const int from_step = 4, const int to_step = 4){
    int to_b, from_b, i;
    const int to_bytes = to_count * to_step;
    const int flatten_factor = to_count % 2 == 0 ? (to_count % 3 == 0 ? 3 : 2) : 1;
    const int divisor_stride = from_step * divisor;

    //This macro allows us to use constants in our loop checks - which in turn lets those loops be unroolled
    //We do this for divisors 2-6, bpp 3-4, and futher flatten 1-3x when allowed

    #define LOOP_IF2(divisor_literal, step, flatten, extra) \
      if (divisor == (divisor_literal) && to_step == (step) && flatten_factor == (flatten)){ \
        for (to_b = 0, from_b = 0; to_b < to_bytes; to_b += flatten * to_step, from_b += flatten * divisor_literal * from_step){\
          for (i = 0; i < (flatten * step); i++){ \
             to[to_b + i] += from[from_b + i] + from[from_b + i + from_step] extra; }}} else


    #define LOOP_IF3(d, s, f, e) LOOP_IF2(d, s, f, + from[from_b + i + 2 * from_step] e)
    #define LOOP_IF4(d, s, f, e) LOOP_IF3(d, s, f, + from[from_b + i + 3 * from_step] e)
    #define LOOP_IF5(d, s, f, e) LOOP_IF4(d, s, f, + from[from_b + i + 4 * from_step] e)
    #define LOOP_IF6(d, s, f, e) LOOP_IF5(d, s, f, + from[from_b + i + 5 * from_step] e)




    LOOP_IF2(2, 3, 1, )
    LOOP_IF2(2, 4, 1, )
    LOOP_IF2(2, 3, 2, )
    LOOP_IF2(2, 4, 2, )
    LOOP_IF2(2, 3, 3, )
    LOOP_IF2(2, 4, 3, )

    LOOP_IF3(3, 3, 1, )
    LOOP_IF3(3, 4, 1, )
    LOOP_IF3(3, 3, 2, )
    LOOP_IF3(3, 4, 2, )
    LOOP_IF3(3, 3, 3, )
    LOOP_IF3(3, 4, 3, )

    LOOP_IF4(4, 3, 1, )
    LOOP_IF4(4, 4, 1, )
    LOOP_IF4(4, 3, 2, )
    LOOP_IF4(4, 4, 2, )
    LOOP_IF4(4, 3, 3, )
    LOOP_IF4(4, 4, 3, )

    LOOP_IF5(5, 3, 1, )
    LOOP_IF5(5, 4, 1, )
    LOOP_IF5(5, 3, 2, )
    LOOP_IF5(5, 4, 2, )
    LOOP_IF5(5, 3, 3, )
    LOOP_IF5(5, 4, 3, )

    LOOP_IF6(6, 3, 1, )
    LOOP_IF6(6, 4, 1, )
    LOOP_IF6(6, 3, 2, )
    LOOP_IF6(6, 4, 2, )
    LOOP_IF6(6, 3, 3, )
    LOOP_IF6(6, 4, 3, )
    {
        for (to_b = 0, from_b = 0; to_b < to_bytes; to_b += to_step, from_b += divisor_stride){
            for (int i = 0; i < to_step; i++){
                for (int f = 0; f < divisor_stride; f += from_step){
                    to[to_b + i] += from[from_b + i + f];
                }
            }
        }
    }
}


static inline int HalveInternal(const BitmapBgraPtr from,
    const BitmapBgraPtr to, const int to_w, const int to_h, const int to_stride, const int divisor)
{

    int to_w_bytes = to_w * to->bpp;
    unsigned short *buffer = (unsigned short *)calloc(to_w_bytes, sizeof(unsigned short));
    if (buffer == NULL) return NULL;

    int y, b, d;
    const unsigned short divisorSqr = divisor * divisor;
    unsigned int shift = 0;
    if (isPowerOfTwo(divisorSqr)){
        shift = intlog2(divisorSqr);
    }

    //TODO: Ensure that from is equal or greater than divisorx to_w and t_h

    for (y = 0; y < to_h; y++){
        memset(buffer, 0, sizeof(short) * to_w_bytes);
        for (d = 0; d < divisor; d++){
            HalveRowByDivisor(from->pixels + (y * divisor + d) * from->stride, buffer, to_w, divisor, from->bpp, to->bpp);
        }
        register unsigned char * dest_line = to->pixels + y * to_stride;

        if (shift == 1){
            for (b = 0; b < to_w_bytes; b++){
                dest_line[b] = buffer[b] >> 1;
            }
        }
        else if (shift == 2){
            for (b = 0; b < to_w_bytes; b++){
                dest_line[b] = buffer[b] >> 2;
            }
        }
        else if (shift == 3){
            for (b = 0; b < to_w_bytes; b++){
                dest_line[b] = buffer[b] >> 3;
            }
        }
        else if (shift > 0){
            for (b = 0; b < to_w_bytes; b++){
                dest_line[b] = buffer[b] >> shift;
            }
        }
        else{
            for (b = 0; b < to_w_bytes; b++){
                dest_line[b] = buffer[b] / divisorSqr;
            }
        }
    }

    free(buffer);

    return 1;
}

static inline int Halve(const BitmapBgraPtr from, const BitmapBgraPtr to, int divisor){
    return HalveInternal(from, to, to->w, to->h, to->stride, divisor);
}


static inline int HalveInPlace(const BitmapBgraPtr from, int divisor)
{
    int to_w = from->w / divisor;
    int to_h = from->h / divisor;
    int to_stride = to_w * from->bpp;
    int r = HalveInternal(from, from, to_w, to_h, to_stride, divisor);
    from->w = to_w;
    from->h = to_h;
    from->stride = to_stride;
    return r;
}


