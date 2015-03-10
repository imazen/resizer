// This is the main DLL file.

#include "stdafx.h"
#include "ImageResizer.Plugins.FastScaling.Tests.Cpp.h"

#include "..\..\Plugins\FastScaling\weighting.h"
#include "..\..\Plugins\FastScaling\bitmap_compositing.h"

bool test_contrib_windows(char *msg)
{
    int bad = -1;
    LineContribType *lct = 0;

    // assumes included edge cases

    InterpolationDetailsPtr cubicFast = CreateInterpolation(InterpolationFilter::Filter_CubicFast);

    unsigned int from_w = 6;
    unsigned int to_w = 3;
    unsigned int corr36[3][2] = { { 0, 1 }, { 2, 3 }, { 4, 5 } };
    lct = ContributionsCalc(to_w, from_w, cubicFast);

    for (int i = 0; i < lct->LineLength; i++)
    if (lct->ContribRow[i].Left != corr36[i][0]) { bad = i; break; }
    else if (lct->ContribRow[i].Right != corr36[i][1]) { bad = i; break; }
    
    if (bad != -1)
    {
        sprintf(msg, "at 6->3 invalid value (%d; %d) at %d, expected (%d; %d)",
            lct->ContribRow[bad].Left,
            lct->ContribRow[bad].Right,
            bad, corr36[bad][0], corr36[bad][1]);
        ContributionsFree(lct);
        return false;
    }
    ContributionsFree(lct);

    from_w = 6;
    to_w = 4;
    unsigned int corr46[4][2] = { { 0, 1 }, { 1, 2 }, { 3, 4 }, { 4, 5 } };
    lct = ContributionsCalc(to_w, from_w, cubicFast);

    for (int i = 0; i < lct->LineLength; i++)
    if (lct->ContribRow[i].Left != corr46[i][0]) { bad = i; break; }
    else if (lct->ContribRow[i].Right != corr46[i][1]) { bad = i; break; }

    if (bad != -1)
    {
        sprintf(msg, "at 6->4 invalid value (%d; %d) at %d, expected (%d; %d)",
            lct->ContribRow[bad].Left,
            lct->ContribRow[bad].Right,
            bad, corr46[bad][0], corr46[bad][1]);
        ContributionsFree(lct);
        return false;
    }
    ContributionsFree(lct);

    return true;
}

bool function_bounded(InterpolationDetailsPtr details, char *msg, double input_start_value, double stop_at_abs, double input_step, double result_low_threshold, double result_high_threshold)
{
    double input_value = input_start_value;
    
    if (abs(input_value) > abs(stop_at_abs))
        return true;

    double result_value = (*details->filter)(details, input_value);
    
    if (result_value < result_low_threshold)
    {
        sprintf(msg + strlen(msg), "value %.4f is below %.4f at x=%.4f", result_value, result_low_threshold, input_value);
        return false;
    }
    else if (result_value > result_high_threshold)
    {
        sprintf(msg + strlen(msg), "value %.4f exceeds %.4f at x=%.4f", result_value, result_high_threshold, input_value);
        return false;
    }
    
    return function_bounded(details, msg, input_value + input_step, stop_at_abs, input_step, result_low_threshold, result_high_threshold);
}

bool function_bounded_bi(InterpolationDetailsPtr details, char *msg, double input_start_value, double stop_at_abs, double input_step, double result_low_threshold, double result_high_threshold)
{
    return function_bounded(details, msg, input_start_value, stop_at_abs, input_step, result_low_threshold, result_high_threshold) &&
        function_bounded(details, msg, input_start_value * -1, stop_at_abs, input_step * -1, result_low_threshold, result_high_threshold);
}

bool test_details(InterpolationDetailsPtr details, char *msg, double expected_first_crossing, double expected_second_crossing, double expected_near0, double near0_threshold, double expected_end)
{
    double top = (*details->filter)(details, 0);

    // Verify peak is at x = 0
    if (!function_bounded_bi(details, msg, 0, expected_end, 0.05, -500, top)) return false;
    
    // Verify we drop below a certain threshold between expected_near0 and expected_second_crossing or expected_end
    if (!function_bounded_bi(details, msg, expected_near0, expected_second_crossing > 0 ? expected_second_crossing : expected_end, 0.05, -500, near0_threshold)) return false;

    //Ensure ended at expected_end
    if (!function_bounded_bi(details, msg, expected_end, expected_end + 1, 0.05, -0.0001, 0.0001)) return false;

    if (expected_first_crossing != 0 && expected_second_crossing != 0){
        //Ensure everything between the two crossings is negative
        if (!function_bounded_bi(details, msg, expected_first_crossing + 0.05, expected_second_crossing - 0.05, 0.05, -500, -0.001)) return false;
        
        //Ensure everything between second crossing and end is positive - if significant
        if (expected_end > expected_second_crossing + 0.1){
            if (!function_bounded_bi(details, msg, expected_second_crossing + 0.05, expected_end - 0.02, 0.02, 0, 500)) return false;

        }
    }
    else{
        //Ensure everything is non-negative
        if (!function_bounded_bi(details, msg, expected_near0, expected_end, 0.05, -0.0001, 500)) return false;

    }


    return true;
}

bool test_filter(InterpolationFilter filter, char *msg, double expected_first_crossing, double expected_second_crossing, double expected_near0, double near0_threshold, double expected_end){
    InterpolationDetailsPtr details = CreateInterpolation(filter);
    sprintf(msg, "Filter=(%d) ",filter);
    bool result = test_details(details, msg,  expected_first_crossing,  expected_second_crossing,  expected_near0,  near0_threshold,  expected_end);
    free(details);
    return result;
}

bool test_weight_distrib(char *msg)
{   
    //These have window = 1, and shouldnt' have negative values. They should also end at 1
    if (!test_filter(InterpolationFilter::Filter_Hermite, msg, 0, 0, 0.99, 0.08, 1)) return false;
    //Also called a linear filter
    if (!test_filter(InterpolationFilter::Filter_Triangle, msg, 0, 0, 0.99, 0.08, 1)) return false;
    //Box should only return a value from -0.5..0.5
    if (!test_filter(InterpolationFilter::Filter_Box, msg, 0, 0, 0.51, 0.001, 0.51)) return false;



    //These should go negative between x=1 and x=2, but should end at x=2
    if (!test_filter(InterpolationFilter::Filter_CatmullRom, msg, 1, 2, 1, 0.08, 2)) return false;
    if (!test_filter(InterpolationFilter::Filter_CubicFast, msg,        1,  2, 1, 0.08, 2)) return false;
    if (!test_filter(InterpolationFilter::Filter_Cubic, msg, 1, 2, 1, 0.08, 2)) return false;

    //BSpline is a smoothing filter, always positive
    if (!test_filter(InterpolationFilter::Filter_CubicBSpline, msg, 0, 0, 1.75, 0.08, 2)) return false;


    if (!test_filter(InterpolationFilter::Filter_Mitchell, msg,         1,  1.75, 1, 0.08, 1.75)) return false;
    if (!test_filter(InterpolationFilter::Filter_Robidoux, msg,         1,  1.7, 1, 0.08, 1.75)) return false;
    if (!test_filter(InterpolationFilter::Filter_RobidouxSharp, msg,    1,  1.8,    1, 0.08, 1.8)) return false;


    //Sinc filters. These have second crossings.
    if (!test_filter(InterpolationFilter::Filter_Lanczos2, msg,         1,  2,      1, 0.08, 2)) return false;
    if (!test_filter(InterpolationFilter::Filter_Lanczos2Sharp, msg,    0.954,  1.86,      1, 0.08, 2)) return false;

    //These should be negative between x=1 and x=2, positive between 2 and 3, but should end at 3

    if (!test_filter(InterpolationFilter::Filter_Lanczos3, msg,         1, 2, 1, 0.1, 3)) return false;
    if (!test_filter(InterpolationFilter::Filter_Lanczos3Sharp, msg,    0.98, 1.9625, 1, 0.1, 2.943)) return false;

    ///
    if (!test_filter(InterpolationFilter::Filter_Lanczos2Windowed, msg, 1, 2, 1, 0.08, 2)) return false;
    if (!test_filter(InterpolationFilter::Filter_Lanczos2SharpWindowed, msg, 0.954, 1.86, 1, 0.08, 2)) return false;

    //These should be negative between x=1 and x=2, positive between 2 and 3, but should end at 3

    if (!test_filter(InterpolationFilter::Filter_Lanczos3Windowed, msg, 1, 2, 1, 0.1, 3)) return false;
    if (!test_filter(InterpolationFilter::Filter_Lanczos3SharpWindowed, msg, 0.98, 1.9625, 1, 0.1, 2.943)) return false;

}

InterpolationDetailsPtr  sample_filter(InterpolationFilter filter, double x_from, double x_to, double *buffer, int samples){
    InterpolationDetailsPtr details = CreateInterpolation(filter);
    if (details == NULL) return NULL;
    for (int i = 0; i < samples; i++){
        double x = (x_to - x_from) * ((double)i / (double)samples) + x_from;
        buffer[i] = details->filter(details, x);
    }
    return details;
}


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

    public ref class TestsCpp
    {
        static Bitmap ^BuildFast(Bitmap ^source, String ^i)
        {
            Config ^c = gcnew Config();

            FastScalingPlugin ^fs = gcnew FastScalingPlugin();
            fs->Install(c);

            Stream ^dest = gcnew MemoryStream();
            ImageJob^ j = gcnew ImageJob();
            j->InstructionsAsString = i; 
            j->Source = source;
            j->Dest = Bitmap::typeid;

            c->Build(j);
            return (Bitmap^)j->Result;
        }

    public:
        [Fact]
        void ContributionsCalcTest()
        {
            char msg[256];
            bool r = test_contrib_windows(msg);
            Assert::True(r, gcnew String(msg));
        }

        [Fact]
        void WeightFuncTest()
        {
            char msg[256];
            bool r = test_weight_distrib(msg);
            Assert::True(r, gcnew String(msg));
        }

        [Fact]
        void AlphaMultTest()
        {
            String ^imgdir = gcnew String("..\\..\\..\\..\\Samples\\Images\\");
            Bitmap^ input = gcnew Bitmap(imgdir + "premult-test.png");
            Bitmap ^output = BuildFast(input, "fastscale=true&width=256");

            Color ^px = output->GetPixel(5, 5);
            Color ^tst = Color::FromArgb(128, 0, 255, 0);
            
            Assert::True(*px == *tst, "Expected: " + tst->ToString() + " Got: " + px->ToString());
        }

        [Fact]
        void GammaTest()
        {
            String ^imgdir = gcnew String("..\\..\\..\\..\\Samples\\Images\\");
            Bitmap ^output = BuildFast(gcnew Bitmap(imgdir + "gamma-test.jpg"), "fastscale=true&width=256");

            Color ^px = output->GetPixel(90, 70);
            Color ^tst = Color::FromArgb(255, 188, 188, 188);

            Assert::True(*px == *tst, "Expected: " + tst->ToString() + " Got: " + px->ToString());
        }

        [Fact]
        void PlotFunctions(){

            int width = 320;
            int height = 200;
            double* buffer = (double *) calloc(width, sizeof(double));
            double window = 3.2;

            for (int i = 0; i < 30; i++){
                InterpolationDetailsPtr details = sample_filter((InterpolationFilter)i, -1 * window, window, buffer, width);
                if (details == NULL) break;

                double vscale = buffer[width / 2] * (height / 3) * -1;

                Bitmap^ b = gcnew Bitmap(width, height);;
                
                Graphics^ g = Graphics::FromImage(b);

                g->DrawLine(Pens::Gray, 0, height / 2, width, height / 2);
                g->DrawLine(Pens::Gray, width / 2, 0, width /2, height);


                for (int j = 0; j <= ceil(window); j++){
                    double offset = (width / 2.0) / window * j;
                    g->DrawLine(Pens::Red, width / 2 + (int)offset, height / 2 - 5, width / 2 + (int)offset, height / 2 + 5);
                    g->DrawLine(Pens::Red, width / 2 - (int)offset, height / 2 - 5, width / 2 - (int)offset, height / 2 + 5);
                }
                double filter_window = (width / 2.0) / window * details->window;
                g->DrawLine(Pens::Blue, width / 2 + (int)filter_window, 0, width / 2 + (int)filter_window, height - 1);
                g->DrawLine(Pens::Blue, width / 2 - (int)filter_window, 0, width / 2 - (int)filter_window, height - 1);

                for (int j = 0; j < width; j++){
                    b->SetPixel(j, (int)round(buffer[j] * vscale) + height / 2, Color::Black);
                }

                b->Save(String::Format("..\\..\\..\\..\\Tests\\ImageResizer.Plugins.FastScaling.Tests.Cpp\\PlotFilter{0}.png", i));
                free(details);
            }

        }

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
            copy_linear_over_srgb(buf, 0, final, 0, h, false);

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
            bgra[2] =0;
            bgra[3] =0;

            linear_to_luv(bgra);
            luv_to_linear(bgra);

            Assert::Equal(0.0f, bgra[0]);
            Assert::Equal(0.0f, bgra[1]);
            Assert::Equal(0.0f, bgra[2]);
            Assert::Equal(0.0f, bgra[3]);

        }
    };
}