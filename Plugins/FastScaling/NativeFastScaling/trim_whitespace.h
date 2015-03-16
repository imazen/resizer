/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */
#include "shared.h"
#pragma once

#ifdef _MSC_VER
#pragma unmanaged
#endif


typedef struct RectStruct {
    uint32_t x1, y1, x2, y2;
} Rect;


typedef struct SearchInfoStruct {
    BitmapBgra * bitmap;
    uint32_t min_x, max_x, min_y, max_y;
    uint32_t w, h;
    uint8_t* buf;
    uint32_t buff_size;
    uint32_t buf_x, buf_y, buf_w, buf_h;
    uint32_t threshold;

} SearchInfo;


#ifdef __cplusplus
extern "C" {
#endif


Rect detect_content(BitmapBgra * b, uint8_t threshold);
int fill_buffer(SearchInfo * __restrict info);
void sobel_scharr_detect(SearchInfo* __restrict info, const int edgeTRBL);
void check_region(const int edgeTRBL, const float x_1_percent, const  float x_2_percent, const float y_1_percent, const  float y_2_percent, SearchInfo* __restrict info);


#ifdef __cplusplus
}
#endif



