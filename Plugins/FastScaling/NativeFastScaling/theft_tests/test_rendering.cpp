#include "fastscaling_private.h"

#define CATCH_CONFIG_MAIN // tell catch to generate main

#include "catch.hpp"
extern "C" {
#include <theft.h>
}
#include <assert.h>
#include <sys/time.h>
#include <string.h>


static bool get_time_seed(theft_seed *seed)
{
    struct timeval tv;
    if (-1 == gettimeofday(&tv, NULL)) { assert(false); }
    *seed = (theft_seed)((tv.tv_sec << 32) | tv.tv_usec);
    /* printf("seed is 0x%016llx\n", *seed); */
    return true;
}

struct TestEnv {
    int max_dimensions;
};

static theft_trial_res before_and_after_should_match(float * bgra_array) {
    float copy[4];
    memcpy(copy, bgra_array, sizeof copy);
    linear_to_luv(copy);
    luv_to_linear(copy);
    for (int i = 0; i < 4; i++) {
        printf("bgra_array is %f\n", bgra_array[i]);
        printf("copy       is %f\n\n", copy[i]);
        if (bgra_array[i] != Approx(copy[i])) {
            return THEFT_TRIAL_FAIL;
        }
    }
    return THEFT_TRIAL_PASS;
}

    
void * allocate_random_bitmap(theft * theft, theft_seed seed, void * input) {
    float * bitmap = (float*)calloc(4, sizeof(float));
    for (int i = 0; i < 4; i++) 
        bitmap[i] = (float)theft_random_double(theft);
    return (void*)bitmap;
}

void free_bitmap(void * bitmap, void * unused) 
{
    free(bitmap);
}



TEST_CASE("Roundtrip RGB<->LUV property", "[fastscaling][thief]") {
    theft_seed seed;
    if (!get_time_seed(&seed)) REQUIRE(false);
    Context context;
    Context_initialize(&context);
    theft * t = theft_init(0);

    theft_type_info bitmap_type_info;
    memset(&bitmap_type_info, 0, sizeof bitmap_type_info);
    bitmap_type_info.alloc = allocate_random_bitmap;
    bitmap_type_info.free = free_bitmap;

    theft_trial_report report;

    theft_cfg cfg;
    memset(&cfg, 0, sizeof cfg);
    cfg.seed = seed;
    cfg.name = __func__;
    cfg.fun = (theft_propfun*)before_and_after_should_match;
    memset(&(cfg.type_info), 0, sizeof cfg.type_info);
    cfg.type_info[0] = &bitmap_type_info;
    cfg.trials = 10;
    cfg.report = &report;
    theft_run_res res = theft_run(t, &cfg);
    theft_free(t);
    printf("\n");
    FAIL("Balle");
    REQUIRE(THEFT_RUN_PASS == res);
}
