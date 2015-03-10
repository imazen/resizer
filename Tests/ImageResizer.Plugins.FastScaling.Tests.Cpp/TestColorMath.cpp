// This is the main DLL file.
#include "stdafx.h"


#pragma managed

using namespace System;
using namespace System::Drawing;
using namespace System::Collections::Specialized;
using namespace System::IO;
using namespace Xunit;
using namespace ImageResizer;
using namespace ImageResizer::Configuration;
using namespace ImageResizer::Plugins::FastScaling;

namespace ImageResizerPluginsFastScalingTestsCpp {

    public ref class TestColorMath
    {

    public:
      

        [Fact]
        void TestLinearToSrgbMath(){
            array<Byte, 1>^ from = gcnew array<Byte, 1>(256);
            array<Byte, 1>^ to = gcnew array<Byte, 1>(256);

            for (int x = 0; x < 256; x++){
                from[x] = x;
                to[x] = uchar_clamp_ff(linear_to_srgb(srgb_to_linear(x / 255.0f)));
            }


            Assert::Equal(from, to);
        }

        [Fact]
        void TestCurvePrecision(){

            //Looks like we need an 11 bit integer to safely store a sRGB byte in linear form.

            int skipfirst = 0; //Skip the first N values, as if we will handle those individually with branches.
            for (int bits = 16; bits > 11; bits--){
                array<Byte, 1>^ from = gcnew array<Byte, 1>(257);
                array<Byte, 1>^ to = gcnew array<Byte, 1>(257);

                for (int x = skipfirst - 1; x < 256; x++){
                    float linear = srgb_to_linear(x / 255.0f);

                    int divisor = (int)pow(2.0, bits);

                    int rounded = lround(linear * divisor);

                    from[x + 1] = x;
                    to[x + 1] = uchar_clamp_ff(linear_to_srgb((float)rounded / (float)divisor));
                }
                from[0] = bits;
                to[0] = bits;

                Assert::Equal(from, to);
            }
        }


        [Fact]
        void TestLookupTables(){
            LookupTables*   t = GetLookupTables();


            Assert::Equal(1, (int)clamp_01_to_04096(t->srgb_to_linear[1]));
            Assert::Equal(2, (int)clamp_01_to_04096(t->srgb_to_linear[2]));

            array<Byte, 1>^ from = gcnew array<Byte, 1>(256);
            array<Byte, 1>^ to = gcnew array<Byte, 1>(256);

            for (int x = 0; x < 256; x++){
                from[x] = x;
                to[x] = t->linear_to_srgb[clamp_01_to_04096(t->srgb_to_linear[x])];
            }


            Assert::Equal(from, to);
        }

        [Fact]
        void TestsRGBRoundtrip(){

            int w = 256;
            int h = 256;

            BitmapBgraPtr bit = CreateBitmapBgra(w, h, true, 4);

            for (int y = 1; y < bit->h; y++){
                for (int x = 0; x < bit->w; x++){
                    uint8_t* pix = bit->pixels + (y * bit->stride) + (x * bit->bpp);

                    *pix = x;
                    *(pix + 1) = x;
                    *(pix + 2) = x;
                    *(pix + 3) = y;
                }
            }

            BitmapBgraPtr final = CreateBitmapBgra(w, h, true, 4);
            BitmapFloatPtr buf = CreateBitmapFloat(w, h, 4, true);

            convert_srgb_to_linear(bit, 0, buf, 0, h);

            demultiply_alpha(buf, 0, h);
            copy_linear_over_srgb(buf, 0, final, 0, h,0,buf->w, false);

            for (int y = 0; y < bit->h; y++){
                for (int x = 0; x < bit->w; x++){
                    uint8_t* from = bit->pixels + (y * bit->stride) + (x * bit->bpp);
                    uint8_t* to = final->pixels + (y * final->stride) + (x * final->bpp);

                    Assert::Equal((*from), (*to));
                    from++; to++;
                    Assert::Equal((*from), (*to));
                    from++; to++;
                    Assert::Equal((*from), (*to));
                    from++; to++;
                    Assert::Equal((*from), (*to));
                    from++; to++;

                }
            }

            //Assert::Equal(0, memcmp(bit->pixels, final->pixels, bit->stride * bit->h));

        }


        [Fact]
        void TestLinearToLuvZero(){
            float bgra[4];

            bgra[0] = 0;
            bgra[1] = 0;
            bgra[2] = 0;
            bgra[3] = 0;

            linear_to_luv(bgra);

            Assert::InRange(bgra[0], 0.0f, 0.0f);
            Assert::InRange(bgra[1], 100.0f, 101.0f);
            Assert::InRange(bgra[2], 100.0f, 101.0f);

        }


        [Fact]
        void TestLuv(){
            float bgra[4];

            bgra[0] = 0.2;
            bgra[1] = 0.2;
            bgra[2] = 0.2;
            bgra[3] = 1;

            linear_to_luv(bgra);
            luv_to_linear(bgra);

            Assert::InRange(bgra[0], 0.199f, 0.201f);
            Assert::InRange(bgra[1], 0.199f, 0.201f);
            Assert::InRange(bgra[2], 0.199f, 0.201f);

        }

        [Fact]
        void TestLuvZero(){
            float bgra[4];

            bgra[0] = 0;
            bgra[1] = 0;
            bgra[2] = 0;
            bgra[3] = 0;

            linear_to_luv(bgra);
            luv_to_linear(bgra);

            Assert::Equal(0.0f, bgra[0]);
            Assert::Equal(0.0f, bgra[1]);
            Assert::Equal(0.0f, bgra[2]);
            Assert::Equal(0.0f, bgra[3]);

        }
    };
}