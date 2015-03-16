/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */

#pragma once

#include <stdint.h>
#include <string.h>
#include <stdlib.h>

typedef struct BitmapFloatStruct BitmapFloat;


#ifdef __cplusplus
extern "C" {
#endif


inline float* create_guassian_kernel(double stdDev, uint32_t radius){
    uint32_t size = radius * 2 + 1;
    float *kernel = (float *)malloc(sizeof(float) * size);
    if (kernel == NULL) return NULL;
    for (uint32_t i = 0; i < size; i++){
        kernel[i] = (float)(IR_GUASSIAN(fabs((float)(radius - i)), stdDev));
    }
    return kernel;
}

inline double sum_of_kernel(float* kernel, uint32_t size){
    double sum = 0;
    for (uint32_t i = 0; i < size; i++){
        sum += kernel[i];
    }
    return sum;
}

inline void normalize_kernel(float* kernel, uint32_t size, float desiredSum){
    float factor = (float)(desiredSum / sum_of_kernel(kernel,size));
    for (uint32_t i = 0; i < size; i++){
        kernel[i] *= factor;
    }
}
inline float* create_guassian_kernel_normalized(double stdDev, uint32_t radius){
    float *kernel = create_guassian_kernel(stdDev, radius);
    if (kernel == NULL) return NULL;
    uint32_t size = radius * 2 + 1;
    normalize_kernel(kernel, size, 1);
    return kernel;
}

inline float* create_guassian_sharpen_kernel(double stdDev, uint32_t radius){
    float *kernel = create_guassian_kernel(stdDev, radius);
    if (kernel == NULL) return NULL;
    uint32_t size = radius * 2 + 1;
    double sum = sum_of_kernel(kernel, size);

    for (uint32_t i = 0; i < size; i++){
        if (i == radius){
            kernel[i] = (float)(2 * sum - kernel[i]);
        }
        else{
            kernel[i] *= -1;
        }
    }
    normalize_kernel(kernel, size,1);
    return kernel;
}


int ConvolveBgraFloatInPlace(
    BitmapFloat * buf, 
    const float *kernel, 
    uint32_t radius, 
    float threshold_min, 
    float threshold_max, 
    uint32_t convolve_channels, 
    uint32_t from_row, 
    int row_count);

#ifdef __cplusplus
}
#endif

