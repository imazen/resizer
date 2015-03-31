/*
* Copyright (c) Imazen LLC.
* No part of this project, including this file, may be copied, modified,
* propagated, or distributed except as permitted in COPYRIGHT.txt.
* Licensed under the GNU Affero General Public License, Version 3.0.
* Commercial licenses available at http://imageresizing.net/
*/
#include "fastscaling.h"
#include "Stdafx.h"
#include "ImageResizer.Plugins.FastScaling.h"
#pragma once
#pragma managed


using namespace System;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;
using namespace ImageResizer::Resizing;
using namespace System::Diagnostics;
using namespace System::Collections::Specialized;
using namespace System::Runtime::InteropServices;

namespace ImageResizer{
    namespace Plugins {
        namespace FastScaling{

#define STATUS_CODE_NAME
#define STATUS_CODE_ENUM_NAME public enum class FastScalingResult

#include "status_code.h"

            enum Rotate{
                RotateNone = 0,
                Rotate90 = 1,
                Rotate180 = 2,
                Rotate270 = 3
            };

            public ref class ConvKernel{
            public:
                property array<float, 1>^ Kernel;
                property int Radius;
                property float MinChangeThreshold;
                property float MaxChangeThreshold;
            };

            public ref class RenderOptions{
            public:
                RenderOptions (){
                    this->Filter = InterpolationFilter::Filter_CubicFast;
                    SamplingWindowOverride = 0;
                    SamplingBlurFactor = 1;
                    SharpeningPercentGoal = 0;
                    MinSamplingWindowToIntegrateSharpening = 1.5;

                    InterpolateLastPercent = 3;
                    HalveOnlyWhenPerfect = true;
                }



                property InterpolationFilter Filter;
                property float SamplingWindowOverride;
                property float SamplingBlurFactor;


                property float SharpeningPercentGoal;
                property float MinSamplingWindowToIntegrateSharpening;

                property array<array<float, 1>^, 1>^ ColorMatrix;

                // If possible to do correctly, halve the image until it is [halve_until] times larger than needed. 3 or greater reccomended. Specify -1 to disable halving.
                property double InterpolateLastPercent;

                //If true, only halve when both dimensions are multiples of the halving factor
                property bool HalveOnlyWhenPerfect;

                property ConvKernel^ KernelA;
                property ConvolutionKernel* KernelA_Struct;
                property ConvKernel^ KernelB;
                property ConvolutionKernel* KernelB_Struct;

                property Rotate Rotation;
                property bool FlipVertical;
                property bool FlipHorizontal;

                property bool RequiresTransposeStep{
                    bool get (){
                        return this->Rotation == Rotate::Rotate270 || this->Rotation == Rotate::Rotate90;
                    }
                }
                property bool RequiresVerticalFlipStep{
                    bool get (){
                        int vflips = 0;
                        if (this->Rotation == Rotate::Rotate270) vflips++;
                        if (this->Rotation == Rotate::Rotate180) vflips++;
                        if (FlipVertical) vflips++;

                        return vflips % 2 == 1;
                    }
                }
                property bool RequiresHorizontalFlipStep{
                    bool get (){
                        int hflips = 0;
                        if (this->Rotation == Rotate::Rotate90) hflips++;
                        if (this->Rotation == Rotate::Rotate180) hflips++;
                        if (FlipHorizontal) hflips++;

                        return hflips % 2 == 1;
                    }
                }
            };
        }
    }
}
