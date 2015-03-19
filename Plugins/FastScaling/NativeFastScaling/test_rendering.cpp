#include "catch.hpp"
#include <thief.h>

TEST_CASE("Roundtrip RGB<->LUV property", "[fastscaling][thief]") {
    theft_seed seed;
    if (!get_time_seed(&sees)) REQUIRE(false);
    test_env env = {.max_dim = 
}
