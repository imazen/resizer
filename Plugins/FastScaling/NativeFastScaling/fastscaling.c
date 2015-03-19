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
    Context context;
    Context_initialize(&context);
    BitmapBgra * source = BitmapBgra_create(&context, sx, sy, true, sbpp);
    BitmapBgra * canvas = BitmapBgra_create(&context, cx, cy, true, cbpp);

    RenderDetails * details = RenderDetails_create(&context);

    details->interpolation = InterpolationDetails_create_from(&context, filter);

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


    Renderer * p = Renderer_create(&context, source, canvas, details);

    Renderer_perform_render(&context, p);

    Renderer_destroy(&context, p);

    BitmapBgra_destroy(&context, source);
    BitmapBgra_destroy(&context, canvas);

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
