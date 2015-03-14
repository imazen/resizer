// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
// This is the main DLL file.
#include "stdafx.h"

#include "fastscaling.h"
#include "math_functions.h"
#include "color_spaces.h"
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


       
       
    };
}