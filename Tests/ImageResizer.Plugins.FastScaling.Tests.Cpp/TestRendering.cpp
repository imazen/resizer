// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
// This is the main DLL file.
#include "stdafx.h"


//TODO: Test compositing with scaling

//TODO: Test blend with matte
//TODO: Test compositing
//TODO: Test blending with scaling
//TODO: Test color matrix without scaling
//TODO: Test rotation and flipping

#include "Stdafx.h"
#include "weighting_test_helpers.h"
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
        void CompositingTest()
        {
            String ^imgdir = gcnew String("..\\..\\..\\..\\Samples\\Images\\");
            Bitmap^ input = gcnew Bitmap(imgdir + "premult-test.png");
            Bitmap ^output = BuildFast(input, "fastscale=true&width=256&bgcolor=blue");

            Color ^px = output->GetPixel(5, 5);
            Color ^tst = Color::FromArgb(255, 0, 188, 187);
            Assert::True(*px == *tst, "Expected: " + tst->ToString() + " Got: " + px->ToString());
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

                double vscale = (2 * height / 3) * -1 / buffer[width / 2];
                int x_axis_y = 2 * height / 3;
                int y_axis_x = width / 2;

                Bitmap^ b = gcnew Bitmap(width, height);;
                
                Graphics^ g = Graphics::FromImage(b);

                g->DrawLine(Pens::LightGray, 0, x_axis_y, width, x_axis_y);
                g->DrawLine(Pens::LightGray, y_axis_x, 0, y_axis_x, height);

                //Plot integers of X
                for (int j = 0; j <= ceil(window); j++){
                    int offset = (width / 2.0) / window * j;
                    g->DrawLine(Pens::Red, y_axis_x + (int)offset + 2, x_axis_y - 8, y_axis_x + (int)offset - 2, x_axis_y + 8);
                    g->DrawLine(Pens::Red, y_axis_x - (int)offset - 2, x_axis_y - 8, y_axis_x - (int)offset + 2, x_axis_y + 8);
                }
                //Plot ideal window bounds
                double filter_window = (width / 2.0) / window * details->window;
                g->DrawLine(Pens::Blue, y_axis_x + (int)filter_window, 0, y_axis_x + (int)filter_window, height - 1);
                g->DrawLine(Pens::Blue, y_axis_x - (int)filter_window, 0, y_axis_x - (int)filter_window, height - 1);

                //Plot filter weights 
                for (int j = 0; j < width; j++){
                    int y = (int)round(buffer[j] * vscale) + x_axis_y; 
                    if (isnan(buffer[j]) || buffer[j] > buffer[width /2]){
                        throw gcnew ArgumentOutOfRangeException("filter","filter produced an invalid value, either NaN, or a peak other than x=0");
                    }
                    b->SetPixel(j, y, Color::Black);
                }

                b->Save(String::Format("..\\..\\..\\..\\Tests\\ImageResizer.Plugins.FastScaling.Tests.Cpp\\PlotFilter{0}.png", i));
                free(details);
            }

        }

    };
}