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
using namespace System::Diagnostics;
using namespace System::Collections::Specialized;
using namespace System::Runtime::InteropServices;

namespace ImageResizer{
    namespace Plugins{
        namespace FastScaling {
            namespace internal_use_only{



                public ref class BitmapOptions{
                public:
                    property Bitmap^ Bitmap;
                    property Rectangle Crop;
                    property bool Readonly;
                    property bool AlphaMeaningful;
                    property bool AllowSpaceReuse;
                    property ImageResizer::Plugins::FastScaling::internal_use_only::BitmapCompositingMode Compositing;
                    property array<Byte>^ Matte_Color;
                };

                public ref class WrappedBitmap{
                public:
                    BitmapOptions^ options;
                    Bitmap^ underlying_bitmap;
                    BitmapData^ locked_bitmap_data;
                    BitmapBgra* bgra;
                    Rectangle crop_window;
                    ExecutionContext^ c;
                public:
                    WrappedBitmap (ExecutionContext^ c, BitmapOptions^ opts)
                    {
                        this->c = c;
                        Bitmap^ source = opts->Bitmap;
                        PixelFormat format = opts->Bitmap->PixelFormat;

                        bool hasAlpha = format == PixelFormat::Format32bppArgb || format == PixelFormat::Format32bppPArgb;
                        if (!hasAlpha && format != PixelFormat::Format32bppRgb && format != PixelFormat::Format24bppRgb){
                            throw gcnew ArgumentOutOfRangeException ("source", "Invalid pixel format " + source->PixelFormat.ToString ());
                        }

                        Rectangle from = opts->Crop;
                        if (from.X < 0 || from.Y < 0 || from.Right > source->Width || from.Bottom > source->Height || from.Width < 1 || from.Height < 1) {
                            throw gcnew ArgumentOutOfRangeException ("crop", "Crop rectangle is outside the bounds of the image");
                        }
                        int sx = from.Width;
                        int sy = from.Height;
                        BitmapBgra* im = BitmapBgra_create_header (c->GetContext (), sx, sy);
                        if (im == NULL) throw gcnew FastScalingException (c);

                        this->underlying_bitmap = source;
                        this->crop_window = from;


                        //LockBits handles cropping for us.
                        this->locked_bitmap_data = source->LockBits (from, opts->Readonly ? ImageLockMode::ReadOnly : ImageLockMode::ReadWrite, source->PixelFormat);
                        im->fmt = hasAlpha ? Bgra32 : Bgr24;
                        im->pixels = (unsigned char *)safe_cast<void *>(this->locked_bitmap_data->Scan0);
                        im->stride = this->locked_bitmap_data->Stride;

                        im->pixels_readonly = opts->Readonly;
                        im->stride_readonly = opts->Readonly || (im->stride - (BitmapPixelFormat_bytes_per_pixel (im->fmt) * im->w) > BitmapPixelFormat_bytes_per_pixel (im->fmt)); //We can never mess with the stride when lockbits is used - unless there is no padding.
                        im->can_reuse_space = !im->stride_readonly && opts->AllowSpaceReuse;

                        im->w = sx;
                        im->h = sy;

                        im->alpha_meaningful = hasAlpha && opts->AlphaMeaningful;

                        im->compositing_mode = (::BitmapCompositingMode)(int)opts->Compositing;
                        if (opts->Matte_Color != nullptr){
                            for (int i = 0; i < Math::Min (4, opts->Matte_Color->Length); i++)
                                im->matte_color[i] = (uint8_t)opts->Matte_Color->GetValue (i); //TODO: Does this cast work right?
                        }

                        this->underlying_bitmap = source;
                        this->bgra = im;

                    }

                    ~WrappedBitmap (){
                        if (locked_bitmap_data != nullptr){
                            underlying_bitmap->UnlockBits (locked_bitmap_data);
                        }
                        BitmapBgra_destroy (c->GetContext (), bgra);
                        bgra = NULL;

                    }

                };



                public ref class WeightingFilter{

                private:
                    InterpolationDetails* details;
                    WeightingFilter (){}
                    Context * c;
                public:
                    double window;
                    static WeightingFilter^ CreateIfValid (int filter);

                    WeightingFilter (int f){
                        c = Context_create ();
                        details = InterpolationDetails_create_from (c, (::InterpolationFilter)f);
                        window = details->window;
                        if (details == nullptr) throw gcnew ArgumentOutOfRangeException ("f");
                    }

                    void SampleFilter (double x_from, double x_to, array<double, 1>^ buffer, int samples){
                        for (int i = 0; i < samples; i++){
                            double x = (x_to - x_from) * ((double)i / (double)samples) + x_from;
                            buffer[i] = details->filter (details, x);
                        }
                    }


                    ~WeightingFilter (){
                        if (c != nullptr)
                            InterpolationDetails_destroy (c, details);
                        details = nullptr;
                        Context_destroy (c);
                        c = nullptr;
                    }
                };

                WeightingFilter^ WeightingFilter::CreateIfValid (int filter){
                    Context * c = Context_create ();
                    InterpolationDetails* d = InterpolationDetails_create_from (c, (::InterpolationFilter)filter);
                    if (d == nullptr) {

                        Context_destroy (c);
                        return nullptr;
                    }
                    WeightingFilter^ f = gcnew WeightingFilter ();
                    f->details = d;
                    f->window = d->window;
                    f->c = c;
                    return f;
                }

            }
        }
    }
}
