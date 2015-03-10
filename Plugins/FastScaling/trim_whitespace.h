#include "Stdafx.h"
#include "shared.h"
#pragma once
#pragma unmanaged


typedef struct Rect{
    uint32_t x1, y1, x2, y2;

}RectStruct;


typedef struct SearchInfo{
    BitmapBgraPtr bitmap;
    uint32_t min_x, max_x, min_y, max_y;
    uint32_t w, h;
    uint8_t* buf;
    uint32_t buff_size;
    uint32_t buf_x, buf_y, buf_w, buf_h;
    uint32_t threshold;

}SearchInfoStruct;



static Rect detect_content(BitmapBgraPtr b, uint8_t threshold){


    SearchInfo info;

    info.buff_size = 2048;
    info.buf = (uint8_t*)ir_malloc(info.buff_size);
    info.max_x = 0;
    info.min_x = b->w;
    info.min_y = b->h;
    info.max_y = 0;
    info.bitmap = b;
    info.threshold = threshold;

    //Let's aim for a minimum dimension of 7px per window
    //We want to glean as much as possible from horizontal strips, as they are faster. 

    //left half, middle, -> 
    check_region(4, 0, 0.5, 0.5, 0.5, &info);
    //right half, middle, <-
    check_region(2, 0.5, 1, 0.5, 0.5, &info);

    //left half, bottom third ->
    check_region(4, 0, 0.5, 0.677, 0.677, &info);
    //right half, bottom third -<
    check_region(2, 0.5, 1, 0.677, 0.677, &info);
    //left half, top third ->
    check_region(4, 0, 0.5, 0.333, 0.333, &info);
    //right half, top third -<
    check_region(2, 0.5, 1, 0.333, 0.333, &info);

    //top half, center \/
    check_region(1, 0.5, 0.5, 0, 0.5, &info);
    //top half, right third
    check_region(1, 0.677, 0.677, 0, 0.5, &info);
    //top half, left third.
    check_region(1, 0.333, 0.333, 0, 0.5, &info);

    //bottom half, center \/
    check_region(3, 0.5, 0.5, 0.5, 1, &info);
    //bottom half, right third
    check_region(3, 0.677, 0.677, 0.5, 1, &info);
    //bottom half, left third.
    check_region(3, 0.333, 0.333, 0.5, 1, &info);


    //We should now have a good idea of where boundaries lie. However... if it seems that more than 25% is whitespace, we should do a different type of scan.
    long area_to_scan_separately = info.min_x * info.h + info.min_y * info.w + (info.w - info.max_x) * info.h + (info.h - info.max_y) * info.h;

    if (area_to_scan_separately > info.h * info.w){
        check_region(0, 0, 1, 0, 1, &info); //Just scan it all at once, non-directionally
    }
    else{

        //Finish by scanning everything that is left. Should be a smaller set.
        //Corners will overlap, and be scanned twice, if they are whitespace. 
        check_region(1, 0, 1, 0, 1, &info);
        check_region(4, 0, 1, 0, 1, &info);
        check_region(2, 0, 1, 0, 1, &info);
        check_region(3, 0, 1, 0, 1, &info);
    }



    Rect result;
    result.x1 = info.min_x;
    result.y1 = info.min_y;
    result.y2 = info.max_y;
    result.x2 = info.max_x;

    ir_free(info.buf);
}
static int fill_buffer(SearchInfo* __restrict info){
    /* Red: 0.299;
    Green: 0.587;
    Blue: 0.114;
    */
    const uint32_t w = info->buf_w;
    const uint32_t h = info->buf_h;
    const uint32_t remnant = info->bitmap->stride - (info->bitmap->bpp * w);
    uint8_t  const  * __restrict bgra = info->bitmap->pixels + (info->bitmap->stride * info->buf_y) + (info->bitmap->bpp * info->buf_x);
    const uint8_t channels = info->bitmap->bpp;
    if (channels == 4 && info->bitmap->alpha_meaningful){
        uint32_t buf_ix = 0;
        for (uint32_t y = 0; y < h; y++){
            for (uint32_t x = 0; x < w; x++){
                info->buf[buf_ix] = (114 * bgra[0] + 587 * bgra[1] + 299 * bgra[2]) * bgra[3] / 255000;
                bgra += 4;
                buf_ix++;
            }
            bgra += remnant;
        }
    }
    else if (channels == 3 || (channels == 4 && info->bitmap->alpha_meaningful)){
        uint32_t buf_ix = 0;
        for (uint32_t y = 0; y < h; y++){
            for (uint32_t x = 0; x < w; x++){
                info->buf[buf_ix] = (114 * bgra[0] + 587 * bgra[1] + 299 * bgra[2]) / 255000;
                bgra += channels;
                buf_ix++;
            }
            bgra += remnant;
        }
    }
    else {
        uint32_t buf_ix = 0;
        for (uint32_t y = 0; y < h; y++){
            for (uint32_t x = 0; x < w; x++){
                uint32_t sum = 0;
                for (uint8_t ch = 0; ch < channels; ch++)
                    sum += bgra[ch];
                info->buf[buf_ix] = sum / channels;
                bgra += channels;
                buf_ix++;
            }
            bgra += remnant;
        }
    }
}



static void sobel_scharr_detect(SearchInfo* __restrict info, const int edgeTRBL){
    #define COEFFA = 3
    #define COEFFB = 10;
    const uint32_t w = info->buf_w;
    const uint32_t h = info->buf_h;
    const uint32_t y_end = h - 1;
    const uint32_t x_end = w - 1;
    const uint32_t threshold = info->threshold;

    uint8_t * __restrict buf = info->buf;
    uint32_t buf_ix = 0;
    for (uint32_t y = 1; y < y_end; y++){
        for (uint32_t x = 1; x < x_end; x++){
            const int gx = -3 * buf[buf_ix - w - 1] + -10 * buf[buf_ix - 1] + -3 * buf[buf_ix + w - 1] + +3 * buf[buf_ix - w + 1] + 10 * buf[buf_ix + 1] +  3 * buf[buf_ix + w + 1];
            const int gy = 3 * buf[buf_ix - w - 1] + 10 * (buf[buf_ix - w]) + 3 * buf[buf_ix - w + 1] + -3 * buf[buf_ix + w - 1] + -10 * (buf[buf_ix + w]) + -3 * buf[buf_ix + w + 1];
            const int value = abs(gx) + abs(gy);
            if (value > threshold){
                const uint32_t x1 = info->buf_x + x - 1;
                const uint32_t x2 = info->buf_x + x + 1;
                const uint32_t y1 = info->buf_y + y - 1;
                const uint32_t y2 = info->buf_y + y + 1;


                if (x1 < info->min_x){
                    info->min_x = x1;
                }
                if (x2 > info->max_x){
                    info->max_x = x2;
                }
                if (y1 < info->min_y){
                    info->min_y = y1;
                }
                if (y2 > info->max_y){
                    info->max_y = y2;
                }
            }
            buf_ix++;
        }
    }



}



static void check_region(const int edgeTRBL, const float x_1_percent, const  float x_2_percent, const float y_1_percent, const  float y_2_percent, SearchInfo* __restrict info){


    uint32_t x1 = MAX(0, MIN(info->w, floor(x_1_percent * (float)info->w) - 1));
    uint32_t x2 = MAX(0, MIN(info->w, ceil(x_2_percent * (float)info->w) + 1));

    uint32_t y1 = MAX(0, MIN(info->h, floor(y_1_percent * (float)info->h) - 1));
    uint32_t y2 = MAX(0, MIN(info->h, ceil(y_2_percent * (float)info->h) + 1));

    //Snap the boundary depending on which side we're searching
    if (edgeTRBL == 4) {
        x1 = 0;
        x2 = MIN(x2, info->min_x);
    }
    if (edgeTRBL == 2) {
        x1 = MAX(x1,info->max_x);
        x2 = info->w;
    }
    if (edgeTRBL == 1){
        y1 = 0;
        y2 = MIN(y2, info->min_y);
    }
    if (edgeTRBL == 3){
        y1 = MAX(y1, info->max_y);
        y2 = info->h;
    }
    if (x1 == x2 || y1 == y2) return; //Nothing left to search.

    //Let's make sure that we're searching at least 7 pixels in the perpendicular direction
    uint32_t min_region_width = (edgeTRBL == 2 || edgeTRBL == 4) ? 3 : 7;
    uint32_t min_region_height = (edgeTRBL == 1 || edgeTRBL == 3) ? 3 : 7;

    while (y2 - y1 < min_region_height){
        y1 = MAX(0, y1 - 1);
        y2 = MIN(info->h, y2 + 1);
    }
    while (x2 - x1 < min_region_width){
        x1 = MAX(0, x1 - 1);
        x2 = MIN(info->w, x2 + 1);
    }

    //Now we need to split this section into regions that fit in the buffer. Might as well do it vertically, so our scans are minimal.

    const uint32_t w = x2 - x1;
    const uint32_t h = y2 - y1;

    //If we are doing a full scan, make them wide along the X axis. Otherwise, make them square.
    const uint32_t window_width = MIN(w, (edgeTRBL == 0 ? info->buff_size / 7 : (uint32_t)ceil(sqrt((float)info->buff_size))));
    const uint32_t window_height = MIN(h, info->buff_size / window_width);

    const uint32_t vertical_windows = (uint32_t)ceil((float)h / (float)window_height);
    const uint32_t horizantal_windows = (uint32_t)ceil((float)w / (float)window_width);


    for (uint32_t window_row = 0; window_row < vertical_windows; window_row++){
        for (uint32_t window_column = 0; window_column < horizantal_windows; window_column++){


            info->buf_x = x1 + (window_width * window_column);
            info->buf_y = y1 + (window_height * window_row);

            info->buf_w = MIN(3, x2 - info->buf_x, window_width);
            info->buf_h = MIN(3, y2 - info->buf_y, window_height);
            uint32_t buf_x2 = info->buf_x + info->buf_w;
            uint32_t buf_y2 = info->buf_y + info->buf_h;


            const bool excluded_x = (info->min_x <= info->buf_x && info->max_x >= buf_x2);

            const bool excluded_y = (info->min_y <= info->buf_y && info->max_y >= buf_y2);
            
            if (excluded_x && excluded_y){
                //Entire window has already been excluded
                continue;
            }
            if (excluded_y && info->min_x < buf_x2 && buf_x2 < info->max_x){
                info->buf_w = MAX(3, info->min_x - info->buf_x);
            }
            else if (excluded_y && info->max_x > info->buf_x && info->buf_x > info->min_x){
                info->buf_x = MIN(buf_x2 - 3, info->max_x);
                info->buf_w = buf_x2 - info->buf_x;

            }
            if (excluded_x && info->min_y < buf_y2 && buf_y2 < info->max_y){
                info->buf_h = MAX(3, info->min_y - info->buf_y);
            }
            else if (excluded_x && info->max_y > info->buf_y && info->buf_y > info->min_y){
                info->buf_y = MIN(buf_y2 - 3, info->max_y);
                info->buf_h = buf_y2 - info->buf_y;
            }
            
            if (info->buf_y + info->buf_h > info->h ||
                info->buf_x + info->buf_w > info->w){
                //We're out of bounds on the image somehow. 
                continue; 
            }

            fill_buffer(info);
            sobel_scharr_detect(info, edgeTRBL);
        }
    }

}
         





