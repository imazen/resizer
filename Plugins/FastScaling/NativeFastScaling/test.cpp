#include "catch.hpp"

#include "fastscaling.h"
#include "bitmap_formats.h"
#include "bitmap_compositing.h"
#include "scaling.h"
#include "weighting_test_helpers.h"

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

TEST_CASE("Test contrib windows", "[fastscaling]") {

  char msg[256];
  bool r = test_contrib_windows(msg);

  if (!r) FAIL(msg);
  REQUIRE(r);
}

TEST_CASE("Test Weighting", "[fastscaling]") {

  char msg[256];
  bool r = test_weight_distrib(msg);

  if (!r) FAIL(msg);
  REQUIRE(r);
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
    CHECK(x == uchar_clamp_ff(linear_to_srgb(srgb_to_linear(x / 255.0f))));
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

    BitmapBgra* bit = CreateBitmapBgra(w, h, true, 4);

    for (size_t y = 1; y < bit->h; y++){
      for (size_t x = 0; x < bit->w; x++){
        uint8_t* pix = bit->pixels + (y * bit->stride) + (x * bit->bpp);

        *pix = (uint8_t)x;
        *(pix + 1) = (uint8_t)x;
        *(pix + 2) = (uint8_t)x;
        *(pix + 3) = (uint8_t)y;
      }
    }

    BitmapBgra* final = CreateBitmapBgra(w, h, true, 4);
   // BitmapFloat* buf = CreateBitmapFloat(w, h, 4, true);

    WHEN ("we do stuff") {

      RenderDetails* details = CreateRenderDetails();
      Renderer* r = CreateRenderer(bit, final, details);

      PerformRender(r);
      
      //convert_srgb_to_linear(bit, 0, buf, 0, h);      
      //demultiply_alpha(buf, 0, h);
      //copy_linear_over_srgb(buf, 0, final, 0, h, 0, buf->w, false);
    
      THEN(" and so forth ") {

        for (size_t y = 0; y < bit->h; y++){
          for (size_t x = 0; x < bit->w; x++){
              uint8_t* from = bit->pixels + (y * bit->stride) + (x * bit->bpp);
              uint8_t* to = final->pixels + (y * final->stride) + (x * final->bpp);

              REQUIRE(*from == *to);
              from++; to++;
              REQUIRE(*from == *to);
              from++; to++;
              REQUIRE(*from == *to);
              from++; to++;
              REQUIRE(*from == *to);
              from++; to++;
          }
        }
      }
    }
  }
}
