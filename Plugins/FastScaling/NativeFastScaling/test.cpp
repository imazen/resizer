#include "catch.hpp"

#include "fastscaling.h"
#include "bitmap_formats.h"
#include "bitmap_compositing.h"
#include "scaling.h"
#include "weighting_test_helpers.h"

bool test(int sx, int sy, int sbpp, int cx, int cy, int cbpp, bool transpose, bool flipx, bool flipy, InterpolationFilter filter)
{
    BitmapBgra * source = create_bitmap_bgra(sx, sy, true, sbpp);
    BitmapBgra * canvas = create_bitmap_bgra(cx, cy, true, cbpp);

    RenderDetails * details = create_render_details();

    details->interpolation = CreateInterpolation(filter);

    details->sharpen_percent_goal = 50;
    details->post_flip_x = flipx;
    details->post_flip_y = flipy;
    details->post_transpose = transpose;


    Renderer * p = create_renderer(source, canvas, details);

    perform_render(p);

    destroy_renderer(p);

    DestroyBitmapBgra(source);
    DestroyBitmapBgra(canvas);

    FreeLookupTables();
    return true;
}

TEST_CASE( "Render without crashing", "[fastscaling]") {
    REQUIRE( test(400,300,4,200,40,4,false,false,false,(InterpolationFilter)0) );
}

TEST_CASE( "Render - upscale", "[fastscaling]") {
    REQUIRE(test(200, 40, 4, 500, 300, 4, false, false, false, (InterpolationFilter)0));
}

TEST_CASE("Render - downscale 24->32", "[fastscaling]") {
    REQUIRE(test(400, 200, 3, 200, 100, 4, false, false, false, (InterpolationFilter)0));
}

TEST_CASE("Render and rotate", "[fastscaling]") {
    REQUIRE(test(200, 40, 4, 500, 300, 4,true,true,true,(InterpolationFilter)0));
}

TEST_CASE("Test contrib windows", "[fastscaling]") {

    char msg[256];
    bool r = test_contrib_windows(msg);

    if (!r) FAIL(msg);
    REQUIRE(r);
}

TEST_CASE("Test Weighting", "[fastscaling]") {

    char msg[256];
 

    //These have window = 1, and shouldnt' have negative values. They should also end at 1
    CHECK(test_filter(InterpolationFilter::Filter_Hermite, msg, 0, 0, 0.99, 0.08, 1) == nullptr);
    //Also called a linear filter
    CHECK(test_filter(InterpolationFilter::Filter_Triangle, msg, 0, 0, 0.99, 0.08, 1) == nullptr);
    //Box should only return a value from -0.5..0.5
    CHECK(test_filter(InterpolationFilter::Filter_Box, msg, 0, 0, 0.51, 0.001, 0.51) == nullptr);



    //These should go negative between x=1 and x=2, but should end at x=2
    CHECK(test_filter(InterpolationFilter::Filter_CatmullRom, msg, 1, 2, 1, 0.08, 2) == nullptr);
    CHECK(test_filter(InterpolationFilter::Filter_CubicFast, msg, 1, 2, 1, 0.08, 2) == nullptr);
    CHECK(test_filter(InterpolationFilter::Filter_Cubic, msg, 1, 2, 1, 0.08, 2) == nullptr);

    //BSpline is a smoothing filter, always positive
    CHECK(test_filter(InterpolationFilter::Filter_CubicBSpline, msg, 0, 0, 1.75, 0.08, 2) == nullptr);

    CHECK(test_filter(InterpolationFilter::Filter_Mitchell, msg, 1.0f, 1.75f, 1, 0.08, 1.75) == nullptr);


    CHECK(test_filter(InterpolationFilter::Filter_Robidoux, msg, 1, 1.65, 1, 0.08, 1.75) == nullptr);
    CHECK(test_filter(InterpolationFilter::Filter_RobidouxSharp, msg, 1, 1.8, 1, 0.08, 1.8) == nullptr);


    //Sinc filters. These have second crossings.
    CHECK(test_filter(InterpolationFilter::Filter_Lanczos2, msg, 1, 2, 1, 0.08, 2) == nullptr);
    CHECK(test_filter(InterpolationFilter::Filter_Lanczos2Sharp, msg, 0.954, 1.86, 1, 0.08, 2) == nullptr);

    //These should be negative between x=1 and x=2, positive between 2 and 3, but should end at 3

    CHECK(test_filter(InterpolationFilter::Filter_Lanczos3, msg, 1, 2, 1, 0.1, 3) == nullptr);
    CHECK(test_filter(InterpolationFilter::Filter_Lanczos3Sharp, msg, 0.98, 1.9625, 1, 0.1, 2.943) == nullptr);

    ///
    CHECK(test_filter(InterpolationFilter::Filter_Lanczos2Windowed, msg, 1, 2, 1, 0.08, 2) == nullptr);

    CHECK(test_filter(InterpolationFilter::Filter_Lanczos2SharpWindowed, msg, 0.954, 1.86, 1, 0.08, 2) == nullptr);

    //These should be negative between x=1 and x=2, positive between 2 and 3, but should end at 3

    CHECK(test_filter(InterpolationFilter::Filter_Lanczos3Windowed, msg, 1, 2, 1, 0.1, 3) == nullptr);


    CHECK(test_filter(InterpolationFilter::Filter_Lanczos3SharpWindowed, msg, 0.98, 1.9625, 1, 0.1, 2.943) == nullptr);

}


TEST_CASE("Test Linear RGB 000 -> LUV ", "[fastscaling]") {
    float bgra[4] = { 0, 0, 0, 0 };

    linear_to_luv(bgra);

    CHECK(bgra[0] == 0.0f);
    CHECK(bgra[1] == 100.0f);
    CHECK(bgra[2] == 100.0f);
}

TEST_CASE("Roundtrip RGB<->LUV 0.2,0.2,0.2 ", "[fastscaling]") {
    float bgra[4] = { 0.2f, 0.2f, 0.2f, 1 };

    linear_to_luv(bgra);
    luv_to_linear(bgra);

    CHECK(bgra[0] == Approx(0.2f));
    CHECK(bgra[1] == Approx(0.2f));
    CHECK(bgra[2] == Approx(0.2f));
}


TEST_CASE("Roundtrip sRGB<->linear RGB<->LUV", "[fastscaling]") {
    for (int x = 0; x < 256; x++){
	CHECK(x == uchar_clamp_ff(linear_to_srgb(srgb_to_linear((float)x / 255.0f))));
    }
}




TEST_CASE("Roundtrip RGB<->LUV 0,0,0,0 ", "[fastscaling]") {
    float bgra[4] = { 0, 0, 0, 0 };

    linear_to_luv(bgra);
    luv_to_linear(bgra);

    CHECK(bgra[0] == 0.0f);
    CHECK(bgra[1] == 0.0f);
    CHECK(bgra[2] == 0.0f);
}


SCENARIO("sRGB roundtrip", "[fastscaling]") {
    GIVEN("A 256x256 image, grayscale gradient along the x axis, alpha along the y") {
	int w = 256;
	int h = 256;

	BitmapBgra* bit = create_bitmap_bgra(w, h, true, 4);

	for (size_t y = 1; y < bit->h; y++){
	    for (size_t x = 0; x < bit->w; x++){
		uint8_t* pix = bit->pixels + (y * bit->stride) + (x * bit->bpp);

		*pix = (uint8_t)x;
		*(pix + 1) = (uint8_t)x;
		*(pix + 2) = (uint8_t)x;
		*(pix + 3) = (uint8_t)y;
	    }
	}

	BitmapBgra* final = create_bitmap_bgra(w, h, true, 4);
	// BitmapFloat* buf = CreateBitmapFloat(w, h, 4, true);

	WHEN ("we do stuff") {

	    RenderDetails* details = create_render_details();
	    Renderer* r = create_renderer(bit, final, details);

	    perform_render(r);
      
	    //convert_srgb_to_linear(bit, 0, buf, 0, h);      
	    //demultiply_alpha(buf, 0, h);
	    //copy_linear_over_srgb(buf, 0, final, 0, h, 0, buf->w, false);
    
	    THEN(" and so forth ") {

		bool exact_match = true;
		for (size_t y = 0; y < bit->h; y++){
		    for (size_t x = 0; x < bit->w; x++){
			uint8_t* from = bit->pixels + (y * bit->stride) + (x * bit->bpp);
			uint8_t* to = final->pixels + (y * final->stride) + (x * final->bpp);

			if (*from != *to) exact_match = false;
			from++; to++;
			if (*from != *to) exact_match = false;
			from++; to++;
			if (*from != *to) exact_match = false;
			from++; to++;
			if (*from != *to) exact_match = false;
			from++; to++;
		    }
		}
		REQUIRE(exact_match);
	    }
      
	}
    }
}
