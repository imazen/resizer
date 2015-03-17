/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */
#ifdef _MSC_VER
#pragma unmanaged
#endif

#include "fastscaling.h"
#include <stdio.h>
#include <string.h>

bool test (int sx, int sy, BitmapPixelFormat sbpp, int cx, int cy, BitmapPixelFormat cbpp, bool transpose, bool flipx, bool flipy, InterpolationFilter filter);

bool test (int sx, int sy, BitmapPixelFormat sbpp, int cx, int cy, BitmapPixelFormat cbpp, bool transpose, bool flipx, bool flipy, InterpolationFilter filter)
{
    BitmapBgra * source = create_bitmap_bgra(sx, sy, true, sbpp);
    BitmapBgra * canvas = create_bitmap_bgra(cx, cy, true, cbpp);

    RenderDetails * details = create_render_details();

    details->interpolation = create_interpolation(filter);

    details->sharpen_percent_goal = 50;
    details->post_flip_x = flipx;
    details->post_flip_y = flipy;
    details->post_transpose = transpose;


    float sepia[25] = { .769f, .686f, .534f, 0, 0,
                        .189f, .168f, .131f, 0, 0,
                        0, 0, 0, 1, 0,
                        0, 0, 0, 0, 1,
                        0, 0, 0, 0, 0};

    memcpy( &details->color_matrix_data, &sepia, sizeof sepia);

    details->apply_color_matrix = true;


    Renderer * p = create_renderer(source, canvas, details);

    perform_render(p);

    destroy_renderer(p);

    destroy_bitmap_bgra(source);
    destroy_bitmap_bgra(canvas);

    free_lookup_tables();
    return true;
}



int main(void)
{
	for (int i =0; i < 10; i++){
   		test (4000, 3000, Bgr24, 800, 600, Bgra32, true, true, false, (InterpolationFilter)0);
    	test (4000, 3000, Bgr24, 1600, 1200, Bgra32, false, true, true, (InterpolationFilter)0);
    	test (1200, 800, Bgra32, 200, 150, Bgra32, false, false, false, (InterpolationFilter)0);
    }
    return 0;
    
}
