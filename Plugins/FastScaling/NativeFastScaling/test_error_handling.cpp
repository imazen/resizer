#include "catch.hpp"
#include "fastscaling_private.h"

static std::ostream& operator<<(std::ostream& out, const BitmapFloat & bitmap_float)
{
    return out << "BitmapFloat: w:" << bitmap_float.w << " h: " << bitmap_float.h << " channels:" << bitmap_float.channels << '\n';
}

TEST_CASE("Argument checking for convert_sgrp_to_linear", "[error_handling]") {
    Context context;
    Context_initialize(&context);
    BitmapBgra * src = create_bitmap_bgra(&context, 2, 3, true, Bgra32);
    char error_msg[1024];
    CAPTURE(Context_last_error_message(&context, error_msg, sizeof error_msg));
    REQUIRE(src != NULL);
    BitmapFloat * dest = create_bitmap_float(1, 1, 4, false);
    convert_srgb_to_linear(src, 3, dest, 0, 0);
    destroy_bitmap_bgra(src);
    CAPTURE(*dest);
    REQUIRE(dest->float_count == 4); // 1x1x4 channels
    destroy_bitmap_float(dest);
}

TEST_CASE("Creating BitmapBgra", "[error_handling]") {
    Context context;
    Context_initialize(&context);
    BitmapBgra * source = NULL;
    SECTION("Creating a 1x1 bitmap is valid") {
	source = create_bitmap_bgra(&context, 1, 1, true, (BitmapPixelFormat)2);
	REQUIRE(source != NULL);
	REQUIRE(!Context_has_error(&context));
    }
    SECTION("A 0x0 bitmap is invalid") {
	source = create_bitmap_bgra(&context, 0, 0, true, (BitmapPixelFormat)2);
	REQUIRE(source == NULL);
	REQUIRE(Context_has_error(&context));
	//REQUIRE(Context_error_message(&context));
    }
    SECTION("A huge bitmap is also invalid") {
	source = create_bitmap_bgra(&context, 1, INT_MAX, true, (BitmapPixelFormat)2);
	REQUIRE(source == NULL);
	REQUIRE(Context_has_error(&context));
    }
    destroy_bitmap_bgra(source);	
}
