//#include "Stdafx.h"

#include "unmanaged_renderer.h"

int test(int sx, int sy, int sbpp, int cx, int cy, int cbpp, InterpolationFilter filter){


  BitmapBgraPtr source = CreateBitmapBgra(sx,sy,true,sbpp);
  BitmapBgraPtr canvas = CreateBitmapBgra(cx,cy,true,cbpp);

  RenderDetailsPtr details = CreateRenderDetails();

  details->interpolation = CreateInterpolation(filter);

  details->sharpen_percent_goal = 50;


  RendererPtr p = CreateRenderer(source, canvas, details);

  PerformRender(p);

  DestroyRenderer(p);

  DestroyBitmapBgra(source);
  DestroyBitmapBgra(canvas);

  FreeLookupTables();
}


int main(int argc, char * argv[]) 
{

    test(4000,3000,4,200,40,4,(InterpolationFilter)0);
  
    printf("flesk\n");
    return 0;
}
