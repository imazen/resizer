#include "Stdafx.h"
#include "shared.h"
#pragma once
#pragma unmanaged


float* create_guassian_kernel(double stdDev, double radius){
    int size = radius * 2 + 1;
    float *kernel = (float *)malloc(sizeof(float) * size);
    for (int i = 0; i < size; i++){
        kernel[i] = IR_GUASSIAN(i - radius, stdDev);
    }
    return kernel;
}

double sum_of_kernel(float* kernel, int size){
    double sum = 0;
    for (int i = 0; i < size; i++){
        sum += kernel[i];
    }
    return sum;
}

void normalize_kernel(float* kernel, int size, double desiredSum){
    double factor = desiredSum / sum_of_kernel(kernel,size);
    for (int i = 0; i < size; i++){
        kernel[i] *= factor;
    }
}

float* create_guassian_sharpen_kernel(double stdDev, double radius){
    float *kernel = create_guassian_kernel(stdDev, radius);
    int size = radius * 2 + 1;
    double sum = sum_of_kernel(kernel, size);

    for (int i = 0; i < size; i++){
        if (i == radius){
            kernel[i] = 2 * sum - kernel[i];
        }
        else{
            kernel[i] *= -1;
        }
    }
    normalize_kernel(kernel, size,1);
    return kernel;
}