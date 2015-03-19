#include "catch.hpp"
#include "fastscaling_private.h"

const int MAX_BYTES_PP = 16;

static std::ostream& operator<<(std::ostream& out, const BitmapFloat & bitmap_float)
{
    return out << "BitmapFloat: w:" << bitmap_float.w << " h: " << bitmap_float.h << " channels:" << bitmap_float.channels << '\n';
}

class Fixture {
public:
    size_t last_attempted_allocation_size;
    bool always_return_null;
    size_t allocation_failure_size_threshold;
    size_t allocation_failure_size;
    int allow_successful_allocs;
    int alloc_count;
    int total_successful_allocs;

    static void * _calloc(Context * context, size_t count, size_t element_size, const char * file, int line){
        return ((Fixture *)context->heap._private_state)->calloc(count, element_size);
    }
    static void * _malloc(Context * context, size_t byte_count, const char * file, int line){
        return ((Fixture *)context->heap._private_state)->malloc(byte_count);
    }
    static void  _free(Context * context, void * pointer, const char * file, int line){
        free(pointer);
    }

    void initialize_heap(Context * context) {
        context->heap._private_state = this;
        context->heap._calloc = _calloc;
        context->heap._malloc = _malloc;
        context->heap._free = _free;
        context->heap._context_terminate = NULL;
    }
    Fixture() {
        always_return_null = false;
        allocation_failure_size_threshold = INT_MAX / 4;
        allocation_failure_size = allocation_failure_size_threshold;
        allow_successful_allocs = INT_MAX;
        last_attempted_allocation_size = -1;
        alloc_count = 0;
        total_successful_allocs = 0;

    }

    bool attempt_alloc(size_t byte_count){
        last_attempted_allocation_size = byte_count;
        if (always_return_null) {
            return false;
        }
        if (allocation_failure_size_threshold < last_attempted_allocation_size) {
            return false;
        }
        if (allocation_failure_size == last_attempted_allocation_size) {
            return false;
        }
        if (alloc_count >= allow_successful_allocs) {
            return false;
        }
        alloc_count++;
        total_successful_allocs++;
        return true;
    }
    void * malloc(size_t byte_count) {
        if (attempt_alloc(byte_count)){
            return ::malloc(byte_count);
        }
        return NULL;
    }

    void * calloc(size_t instances, size_t size_of_instance) {
        if (attempt_alloc(instances * size_of_instance)){
            return ::calloc(instances, size_of_instance);
        }
        return NULL;
    }

    void always_fail_allocation() {
        always_return_null = true;
    }

    void fail_allocation_of(size_t byte_count) {
        allocation_failure_size = byte_count;
    }

    void fail_allocation_if_size_larger_than(size_t byte_count) {
        allocation_failure_size_threshold = byte_count;
    }


    void fail_alloc_after(int times) {
        alloc_count = 0;
        allow_successful_allocs = times;
    }
};

TEST_CASE_METHOD(Fixture, "Perform Rendering", "[error_handling]") {
    REQUIRE((2 / 10) == 0);
    Context context;
    Context_initialize(&context);
    initialize_heap(&context);
    BitmapBgra * source = BitmapBgra_create(&context, 4, 4, true, Bgra32);
    BitmapBgra * canvas = BitmapBgra_create(&context, 2, 2, true, Bgra32);
    RenderDetails * details = RenderDetails_create(&context);
    details->interpolation = InterpolationDetails_create_from(&context,Filter_CubicFast);
    details->sharpen_percent_goal = 50;
    details->post_flip_x = true;
    details->post_flip_y = false;
    details->post_transpose = false;
    Renderer * p = Renderer_create(&context, source, canvas, details);

    SECTION("Render failure invalid bitmap dimensions for tmp_im") {
        details->halving_divisor = 5;
        CHECK(Renderer_perform_render(&context, p) == false);
        CHECK(Context_has_error(&context));
        char buffer[1024];
        CAPTURE(Context_error_message(&context, buffer, sizeof(buffer)));
        CHECK(Context_error_reason(&context) == Invalid_BitmapBgra_dimensions);

    }


    Renderer_destroy(&context,p);
    BitmapBgra_destroy(&context,source);
    BitmapBgra_destroy(&context,canvas);
    free_lookup_tables();
}

TEST_CASE_METHOD(Fixture, "Test allocation failure handling", "[error_handling]") {
    using namespace Catch::Generators;
    int fail_alloc_x = GENERATE( between( 0, 9 ) );


    Context context;
    Context_initialize(&context);
    initialize_heap(&context);

    BitmapBgra * source = BitmapBgra_create(&context, 4, 4, true, Bgra32);
    BitmapBgra * canvas = BitmapBgra_create(&context, 2, 2, true, Bgra32);
    RenderDetails * details = RenderDetails_create(&context);
    details->interpolation = InterpolationDetails_create_from(&context,Filter_CubicFast);
    details->sharpen_percent_goal = 50;
    details->post_flip_x = true;
    details->post_flip_y = false;
    details->post_transpose = false;
    Renderer * p = Renderer_create(&context, source, canvas, details);

    // think about strategies to make it easier to pinpoint which allocation should fail
    details->halving_divisor = 2;

    fail_alloc_after(fail_alloc_x);
    bool result = Renderer_perform_render(&context, p);
    CAPTURE(fail_alloc_x);

    CAPTURE(alloc_count);

    CAPTURE(total_successful_allocs);
    CAPTURE(last_attempted_allocation_size);
    CHECK(!result);
    CHECK(Context_has_error(&context));
    char buffer[1024];
    CAPTURE(Context_error_message(&context, buffer, sizeof(buffer)));
    CHECK(Context_error_reason(&context) == Out_of_memory);


    Renderer_destroy(&context,p);
    BitmapBgra_destroy(&context,source);
    BitmapBgra_destroy(&context,canvas);
    free_lookup_tables();
}

TEST_CASE_METHOD(Fixture, "Creating BitmapBgra", "[error_handling]") {
    Context context;
    Context_initialize(&context);
    initialize_heap(&context);
    BitmapBgra * source = NULL;
    SECTION("Creating a 1x1 bitmap is valid") {
        source = BitmapBgra_create(&context, 1, 1, true, (BitmapPixelFormat)2);
        REQUIRE_FALSE(source == NULL);
        REQUIRE_FALSE(Context_has_error(&context));
    }
    SECTION("A 0x0 bitmap is invalid") {
        source = BitmapBgra_create(&context, 0, 0, true, (BitmapPixelFormat)2);
        REQUIRE(source == NULL);
        REQUIRE(Context_has_error(&context));
        REQUIRE(Context_error_reason(&context) == Invalid_BitmapBgra_dimensions);
        //REQUIRE(Context_error_message(&context));
    }
    SECTION("A gargantuan bitmap is also invalid") {
        source = BitmapBgra_create(&context, 1, INT_MAX, true, (BitmapPixelFormat)2);
        REQUIRE(source == NULL);
        REQUIRE(Context_has_error(&context));
        REQUIRE(Context_error_reason(&context) == Invalid_BitmapBgra_dimensions);
    }

    SECTION("A bitmap that exhausts memory is invalid too") {
        always_fail_allocation();
        source = BitmapBgra_create(&context, 1, 1, true, (BitmapPixelFormat)2);
        REQUIRE(source == NULL);
        REQUIRE(Context_has_error(&context));
        REQUIRE(Context_error_reason(&context) == Out_of_memory);
    }
    SECTION("Exhausting memory in the pixel allocation is also handled") {
        fail_allocation_if_size_larger_than(sizeof(BitmapBgra));
        source = BitmapBgra_create(&context, 640, 480, true, (BitmapPixelFormat)2);
        REQUIRE(source == NULL);
        REQUIRE(last_attempted_allocation_size == 640 * 480 * 2); // the failed allocation tried to allocate the pixels
        REQUIRE(Context_has_error(&context));
        REQUIRE(Context_error_reason(&context) == Out_of_memory);
    }
    BitmapBgra_destroy(&context,source);
}

TEST_CASE("Argument checking for convert_sgrp_to_linear", "[error_handling]") {
    Context context;
    Context_initialize(&context);
    BitmapBgra * src = BitmapBgra_create(&context, 2, 3, true, Bgra32);
    char error_msg[1024];
    CAPTURE(Context_error_message(&context, error_msg, sizeof error_msg));
    REQUIRE(src != NULL);
    BitmapFloat * dest = BitmapFloat_create(&context,1, 1, 4, false);
    BitmapBgra_convert_srgb_to_linear(&context, src, 3, dest, 0, 0);
    BitmapBgra_destroy(&context,src);
    CAPTURE(*dest);
    REQUIRE(dest->float_count == 4); // 1x1x4 channels
    BitmapFloat_destroy(&context,dest);
}
