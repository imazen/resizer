#include "fastscaling_private.h"

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

static theft_trial_res before_and_after_should_match() {
    return THEFT_TRIAL_ERROR;
}

    
void * allocate_random_bitmap(theft * theft, theft_seed seed, void * input) {
    return NULL;
}

TEST_CASE("Roundtrip RGB<->LUV property", "[fastscaling][thief]") {
    theft_seed seed;
    if (!get_time_seed(&seed)) REQUIRE(false);
    Context context;
    Context_initialize(&context);
    theft * t = theft_init(0);
    theft_type_info bitmap_type_info;
    bitmap_type_info.alloc = allocate_random_bitmap;
    bitmap_type_info.free = NULL;
    bitmap_type_info.hash = NULL;
    bitmap_type_info.shrink = NULL;
    bitmap_type_info.print = NULL;
    theft_trial_report report;
    theft_cfg cfg;
    memset(&cfg, 0, sizeof cfg);
    cfg.seed = seed;
    cfg.name = __func__;
    cfg.fun = before_and_after_should_match;
    memset(&(cfg.type_info), 0, sizeof cfg.type_info);
    cfg.type_info[0] = &bitmap_type_info;
    cfg.trials = 10000;
    cfg.report = &report;
    theft_run_res res = theft_run(t, &cfg);
    theft_free(t);
    printf("\n");
    REQUIRE(THEFT_RUN_PASS == res);
}
