
#pragma once
#pragma unmanaged


float* create_guassian_kernel(double stdDev, uint32_t radius){
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

void normalize_kernel(float* kernel, uint32_t size, float desiredSum){
    float factor = (float)(desiredSum / sum_of_kernel(kernel,size));
    for (uint32_t i = 0; i < size; i++){
        kernel[i] *= factor;
    }
}
float* create_guassian_kernel_normalized(double stdDev, uint32_t radius){
    float *kernel = create_guassian_kernel(stdDev, radius);
    if (kernel == NULL) return NULL;
    uint32_t size = radius * 2 + 1;
    normalize_kernel(kernel, size, 1);
    return kernel;
}

float* create_guassian_sharpen_kernel(double stdDev, uint32_t radius){
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


static int
ConvolveBgraFloatInPlace(BitmapFloatPtr buf, const float *kernel, const uint32_t radius, float threshold, const uint32_t convolve_channels = 4, const uint32_t from_row = 0, const int row_count = -1){

    if (buf->w < radius + 1) return -2; //Do nothing unless the image is at least half as wide as the kernel.
   
    const uint32_t buffer_count = radius + 1;
    const uint32_t w = buf->w;
    const uint32_t step = buf->channels;

    const uint32_t until_row = row_count < 0 ? buf->h : from_row + row_count;

    const uint32_t ch_used = convolve_channels;

    float* __restrict buffer = (float *)_malloca(sizeof(float) * buffer_count * ch_used);
    if (buffer == NULL) return -1;
    float* __restrict avg = (float *)_malloca(sizeof(float) * ch_used);
    if (avg == NULL) {
        _freea(buffer);  
        return -1;
    }
    

    for (uint32_t row = from_row; row < until_row; row++){

        float* __restrict source_buffer = &buf->pixels[row * buf->float_stride];
        int circular_idx = 0;

        for (uint32_t ndx = 0; ndx < w + buffer_count; ndx++) {
            //Flush old value
            if (ndx >= buffer_count){
                memcpy(&source_buffer[(ndx - buffer_count) * step], &buffer[circular_idx * ch_used], ch_used * sizeof(float));
            }
            //Calculate and enqueue new value
            if (ndx < w){
                const int left = ndx - radius;
                const int right = ndx + radius;
                int i;

                memset(avg, 0, sizeof(float) * ch_used);

                if (left < 0 || right >= (int32_t)w){
                    /* Accumulate each channel */
                    for (i = left; i <= right; i++) {
                        const float weight = kernel[i - left];
                        const uint32_t ix = CLAMP(i, 0, (int32_t)w);
                        for (uint32_t j = 0; j < ch_used; j++)
                            avg[j] += weight * source_buffer[ix * step + j];
                    }
                }
                else{
                    /* Accumulate each channel */
                    for (i = left; i <= right; i++) {
                        const float weight = kernel[i - left];
                        for (uint32_t j = 0; j < ch_used; j++)
                            avg[j] += weight * source_buffer[i * step + j];
                    }
                }

                //Enqueue difference
                memcpy(&buffer[circular_idx * ch_used], avg, ch_used * sizeof(float));

                if (threshold > 0){
                    float change = 0;
                    for (uint32_t j = 0; j < ch_used; j++)
                        change += fabs(source_buffer[ndx * step + j] - avg[j]);

                    if (change < threshold){
                        memcpy(&buffer[circular_idx * ch_used], &source_buffer[ndx * step], ch_used * sizeof(float));
                    }
                }
            }
            circular_idx = (circular_idx + 1) % buffer_count;

        }
    }


    _freea(avg);
    _freea(buffer);
    return 0;
}


