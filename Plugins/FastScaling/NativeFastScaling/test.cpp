#define CATCH_CONFIG_MAIN // tell catch to generate main
#include "catch.hpp"

#include "fastscaling.h"
#include "bitmap_formats.h"
#include "bitmap_compositing.h"
#include "scaling.h"


bool test(int sx, int sy, int sbpp, int cx, int cy, int cbpp, InterpolationFilter filter)
{
    BitmapBgra * source = CreateBitmapBgra(sx, sy, true, sbpp);
    BitmapBgra * canvas = CreateBitmapBgra(cx, cy, true, cbpp);

    RenderDetails * details = CreateRenderDetails();

    details->interpolation = CreateInterpolation(filter);

    details->sharpen_percent_goal = 50;


    Renderer * p = CreateRenderer(source, canvas, details);

    PerformRender(p);

    DestroyRenderer(p);

    DestroyBitmapBgra(source);
    DestroyBitmapBgra(canvas);

    FreeLookupTables();
    return true;
}

TEST_CASE( "Render without crashing", "[fastscaling]") {
    REQUIRE( test(4000,3000,4,200,40,4,(InterpolationFilter)0) );
}

TEST_CASE( "Render with crashing", "[fastscaling]") {
    REQUIRE( test(200, 40, 4, 4000, 3000, 4, (InterpolationFilter)0) );
}


// can't access demultiply_alpha or copy_linear_over_srgb

SCENARIO("sRGB roundtrip", "[fastscaling]") {
    GIVEN("Bla bla") {
	int w = 256;
	int h = 256;

	BitmapBgra* bit = CreateBitmapBgra(w, h, true, 4);

	for (size_t y = 1; y < bit->h; y++){
	    for (size_t x = 0; x < bit->w; x++){
		uint8_t* pix = bit->pixels + (y * bit->stride) + (x * bit->bpp);

		*pix = x;
		*(pix + 1) = x;
		*(pix + 2) = x;
		*(pix + 3) = y;
	    }
	}

	BitmapBgra* final = CreateBitmapBgra(w, h, true, 4);
	BitmapFloat* buf = CreateBitmapFloat(w, h, 4, true);

	WHEN ("we do stuff") {
	    convert_srgb_to_linear(bit, 0, buf, 0, h);	    
	    demultiply_alpha(buf, 0, h);
	    copy_linear_over_srgb(buf, 0, final, 0, h, 0, buf->w, false);
	}
	THEN(" and so forth ") {
	    for (size_t y = 0; y < bit->h; y++){
		for (size_t x = 0; x < bit->w; x++){
		    uint8_t* from = bit->pixels + (y * bit->stride) + (x * bit->bpp);
		    uint8_t* to = final->pixels + (y * final->stride) + (x * final->bpp);

		    // REQUIRE(*from == *to);
		    // from++; to++;
		    // REQUIRE(*from == *to);
		    // from++; to++;
		    // REQUIRE(*from == *to);
		    // from++; to++;
		    // REQUIRE(*from == *to);
		    // from++; to++;
		}
	    }
	}
    }
}
