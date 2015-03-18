#include "catch.hpp"
#include "fastscaling_private.h"

const int MAX_BYTES_PP = 16;

static std::ostream& operator<<(std::ostream& out, const BitmapFloat & bitmap_float)
{
    return out << "BitmapFloat: w:" << bitmap_float.w << " h: " << bitmap_float.h << " channels:" << bitmap_float.channels << '\n';
}

class Fixture {
public:
    static Fixture * self;
    size_t last_allocation;
    bool its_always_return_null;
    size_t allocation_failure_threshold;
    size_t allocation_failure_size;
    static void * calloc_shim(size_t instances, size_t size_of_instance) {
	return self->calloc(instances, size_of_instance);
    }

    Fixture() {
	self = this;
	its_always_return_null = false;
	allocation_failure_threshold = INT_MAX / 4;
	allocation_failure_size = allocation_failure_threshold;
    last_allocation = 0;
    }

    void * calloc(size_t instances, size_t size_of_instance) {
	last_allocation = instances * size_of_instance;
	if (its_always_return_null) {
	    return NULL;
	}
	if (allocation_failure_threshold < last_allocation) {
	    return NULL;
	}
	if (allocation_failure_size == last_allocation) {
	    return NULL;
	}
	return ::calloc(instances, size_of_instance);
    }

    void always_fail_allocation() {
	its_always_return_null = true;
    }

    void fail_allocation_of(size_t byte_count) {
	allocation_failure_size = byte_count;
    }

    void fail_allocation_if_size_larger_than(size_t byte_count) {
	allocation_failure_threshold = byte_count;
    }
};

Fixture * Fixture::self = NULL;


TEST_CASE_METHOD(Fixture, "Perform Rendering", "[ error_handling ]") {
    REQUIRE((2 / 10) == 0);
    Context context;
    Context_initialize(&context);
    context.calloc = Fixture::calloc_shim;
    BitmapBgra * source = create_bitmap_bgra(&context, 4, 4, true, Bgra32);
    BitmapBgra * canvas = create_bitmap_bgra(&context, 2, 2, true, Bgra32);
    RenderDetails * details = create_render_details();
    details->interpolation = create_interpolation(Filter_CubicFast);
    details->sharpen_percent_goal = 50;
    details->post_flip_x = true;
    details->post_flip_y = false;
    details->post_transpose = false;
    Renderer * p = create_renderer(source, canvas, details);

    SECTION("Render failure invalid bitmap dimensions for tmp_im") {
	details->halving_divisor = 5;
	REQUIRE(perform_render(&context, p) < 0);
	REQUIRE(Context_has_error(&context));
    	REQUIRE(Context_error_reason(&context) == Invalid_BitmapBgra_dimensions);
    }

    // SECTION("Render failure due to buffer allocation failure in HalveInternal") {
    // 	fail_allocation_of(canvas->w * sizeof(short));
    // 	int result = perform_render(&context, p);
    // 	REQUIRE(last_allocation == 0);
    // 	REQUIRE(result < 0);
    // 	REQUIRE(Context_has_error(&context));
    // 	REQUIRE(Context_error_reason(&context) == Out_of_memory);	
    // }
    destroy_renderer(p);

    destroy_bitmap_bgra(source);
    destroy_bitmap_bgra(canvas);
    free_lookup_tables();
}

TEST_CASE_METHOD(Fixture, "Creating BitmapBgra", "[ error_handling ]") {
    Context context;
    Context_initialize(&context);
    context.calloc = Fixture::calloc_shim;
    BitmapBgra * source = NULL;
    SECTION("Creating a 1x1 bitmap is valid") {
    	source = create_bitmap_bgra(&context, 1, 1, true, (BitmapPixelFormat)2);
    	REQUIRE_FALSE(source == NULL);
    	REQUIRE_FALSE(Context_has_error(&context));
    }
    SECTION("A 0x0 bitmap is invalid") {
    	source = create_bitmap_bgra(&context, 0, 0, true, (BitmapPixelFormat)2);
    	REQUIRE(source == NULL);
    	REQUIRE(Context_has_error(&context));
    	REQUIRE(Context_error_reason(&context) == Invalid_BitmapBgra_dimensions);
    	//REQUIRE(Context_error_message(&context));
    }
    SECTION("A gargantuan bitmap is also invalid") {
	source = create_bitmap_bgra(&context, 1, INT_MAX, true, (BitmapPixelFormat)2);
	REQUIRE(source == NULL);
	REQUIRE(Context_has_error(&context));
	REQUIRE(Context_error_reason(&context) == Invalid_BitmapBgra_dimensions);
    }
	    
    SECTION("A bitmap that exhausts memory is invalid too") {
	always_fail_allocation();
	source = create_bitmap_bgra(&context, 1, 1, true, (BitmapPixelFormat)2);
	REQUIRE(source == NULL);
	REQUIRE(Context_has_error(&context));
	REQUIRE(Context_error_reason(&context) == Out_of_memory);	
    }
    SECTION("Exhausting memory in the pixel allocation is also handled") {
	fail_allocation_if_size_larger_than(sizeof(BitmapBgra));
	source = create_bitmap_bgra(&context, 640, 480, true, (BitmapPixelFormat)2);
	REQUIRE(source == NULL);
	REQUIRE(last_allocation == 640 * 480 * 2); // the failed allocation tried to allocate the pixels
	REQUIRE(Context_has_error(&context));
	REQUIRE(Context_error_reason(&context) == Out_of_memory);		
    }
    destroy_bitmap_bgra(source);	
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
