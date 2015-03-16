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
    namespace Plugins{
        namespace FastScaling {

            enum Rotate{
                RotateNone = 0,
                Rotate90 = 1,
                Rotate180 = 2,
                Rotate270 = 3
            };

            public ref class BitmapOptions{
            public:
                property Bitmap^ Bitmap;
                property Rectangle Crop;
                property bool Readonly;
                property bool AlphaMeaningful;
                property bool AllowSpaceReuse;
                property BitmapCompositingMode Compositing;
                property array<Byte>^ Matte_Color;
            };

            public ref class WrappedBitmap{
            public:
                BitmapOptions^ options;
                Bitmap^ underlying_bitmap;
                BitmapData^ locked_bitmap_data;
                BitmapBgra* bgra;
                Rectangle crop_window;

            public:
                WrappedBitmap(BitmapOptions^ opts)
                {

                    Bitmap^ source = opts->Bitmap;
                    PixelFormat format = opts->Bitmap->PixelFormat;

                    bool hasAlpha = format == PixelFormat::Format32bppArgb || format == PixelFormat::Format32bppPArgb;
                    if (!hasAlpha && format != PixelFormat::Format32bppRgb && format != PixelFormat::Format24bppRgb){
                        throw gcnew ArgumentOutOfRangeException("source", "Invalid pixel format " + source->PixelFormat.ToString());
                    }

                    Rectangle from = opts->Crop;
                    if (from.X < 0 || from.Y < 0 || from.Right > source->Width || from.Bottom > source->Height || from.Width < 1 || from.Height < 1) {
                        throw gcnew ArgumentOutOfRangeException("from");
                    }
                    int sx = from.Width;
                    int sy = from.Height;

                    BitmapBgra* im = create_bitmap_bgra_header(sx, sy);
                    if (im == NULL) throw gcnew InvalidOperationException("Failed to create Bgra Header");

                    this->underlying_bitmap = source;
                    this->crop_window = from;


                    //LockBits handles cropping for us.
                    this->locked_bitmap_data = source->LockBits(from, opts->Readonly ? ImageLockMode::ReadOnly : ImageLockMode::ReadWrite, source->PixelFormat);
                    im->bpp = hasAlpha ? 4 : 3;
                    im->pixels = (unsigned char *)safe_cast<void *>(this->locked_bitmap_data->Scan0);
                    im->stride = this->locked_bitmap_data->Stride;

                    im->pixels_readonly = opts->Readonly;
                    im->stride_readonly = opts->Readonly || (im->stride - (im->bpp * im->w) > im->bpp); //We can never mess with the stride when lockbits is used - unless there is no padding.
                    im->can_reuse_space = !im->stride_readonly && opts->AllowSpaceReuse;

                    im->w = sx;
                    im->h = sy;

                    im->alpha_meaningful = hasAlpha && opts->AlphaMeaningful;

                    im->compositing_mode = opts->Compositing;
                    if (opts->Matte_Color == nullptr){
                        im->matte_color = 0;
                    }
                    else{
                        int length = opts->Matte_Color->Length;
                        im->matte_color = (uint8_t*)malloc(length);
                        for (int i = 0; i < length; i++)
                            im->matte_color[i] = (uint8_t)opts->Matte_Color->GetValue(i); //TODO: Does this cast work right?
                    }

                    this->underlying_bitmap = source;
                    this->bgra = im;

                }

                ~WrappedBitmap(){
                    if (locked_bitmap_data != nullptr){
                        underlying_bitmap->UnlockBits(locked_bitmap_data);
                    }
                    if (bgra != NULL){
                        if (bgra->matte_color)
                            delete bgra->matte_color;
                        destroy_bitmap_bgra(bgra);
                        bgra = NULL;
                    }

                }

            };



            public ref class WeightingFilter{

            private:
              InterpolationDetails* details;
              WeightingFilter(){}
            public:
              double window;
              static WeightingFilter^ CreateIfValid(InterpolationFilter filter);

              WeightingFilter(InterpolationFilter f){
                details = create_interpolation(f);
                window = details->window;
                if (details == nullptr) throw gcnew ArgumentOutOfRangeException("f");
              }

              void SampleFilter( double x_from, double x_to, array<double,1>^ buffer, int samples){
                for (int i = 0; i < samples; i++){
                  double x = (x_to - x_from) * ((double)i / (double)samples) + x_from;
                  buffer[i] = details->filter(details, x);
                }
              }


              ~WeightingFilter(){
                free(details);
              }
            };

             WeightingFilter^ WeightingFilter::CreateIfValid(InterpolationFilter filter){
              InterpolationDetails* d = create_interpolation(filter);
              if (d == nullptr) return nullptr;
              WeightingFilter^ f = gcnew WeightingFilter();
              f->details = d;
              f->window = d->window;
              return f;
            }

            public ref class RenderOptions{
            public:
                RenderOptions(){
                    this->Filter = InterpolationFilter::Filter_CubicFast;
                    SamplingWindowOverride = 0;
                    SamplingBlurFactor = 1;
                    SharpeningPercentGoal = 0;
                    MinSamplingWindowToIntegrateSharpening = 1.5;

                    InterpolateLastPercent = 3;
                    HalveOnlyWhenPerfect = true;
                    ConvolutionA_MaxChangeThreshold = 0;
                    ConvolutionA_MinChangeThreshold = 0;
                    ConvolutionB_MaxChangeThreshold = 0;
                    ConvolutionB_MinChangeThreshold = 0;
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

                property array<float, 1>^ ConvolutionA;

                property float ConvolutionA_MinChangeThreshold;
                property float ConvolutionA_MaxChangeThreshold;

                property array<float, 1>^ ConvolutionB;
                property float ConvolutionB_MinChangeThreshold;
                property float ConvolutionB_MaxChangeThreshold;

                property Rotate Rotation;
                property bool FlipVertical;
                property bool FlipHorizontal;

                property bool RequiresTransposeStep{
                    bool get(){
                        return this->Rotation == Rotate::Rotate270 || this->Rotation == Rotate::Rotate90;
                    }
                }
                property bool RequiresVerticalFlipStep{
                    bool get(){
                        int vflips = 0;
                        if (this->Rotation == Rotate::Rotate270) vflips++;
                        if (this->Rotation == Rotate::Rotate180) vflips++;
                        if (FlipVertical) vflips++;

                        return vflips % 2 == 1;
                    }
                }
                property bool RequiresHorizontalFlipStep{
                    bool get(){
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
