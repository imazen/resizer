
#include "fastscaling.h"
#include <stdio.h>

static int test(int sx, int sy, int sbpp, int cx, int cy, int cbpp, InterpolationFilter filter)
{
    BitmapBgra * source = create_bitmap_bgra(sx, sy, true, sbpp);
    BitmapBgra * canvas = create_bitmap_bgra(cx, cy, true, cbpp);

    RenderDetails * details = create_render_details();

    details->interpolation = CreateInterpolation(filter);

    details->sharpen_percent_goal = 50;


    Renderer * p = create_renderer(source, canvas, details);

    perform_render(p);

    destroy_renderer(p);

    DestroyBitmapBgra(source);
    DestroyBitmapBgra(canvas);

    FreeLookupTables();
}


int main(void) 
{
    test(4000,3000,4,200,40,4,(InterpolationFilter)0);
  
    printf("flesk\n");
    return 0;
}
