#include "shared.h"
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

            public ref class WrappedBitmap{
            public: 
                Bitmap^ underlying_bitmap; 
                BitmapData^ locked_bitmap_data; 
                BitmapBgraPtr bgra;
                Rectangle crop_window;
                WrappedBitmap(Bitmap^ b, BitmapBgraPtr bb)
                {
                    this->underlying_bitmap = b;
                    this->bgra = bb;
                    this->bgra->pixels_readonly = true;
                    this->bgra->stride_readonly = true;
                    this->bgra->compositing_mode = ::BitmapCompositingMode::Blend_with_self;
                    this->bgra->matte_color = 0;
                }

                ~WrappedBitmap(){
                    if (locked_bitmap_data != nullptr){
                        underlying_bitmap->UnlockBits(locked_bitmap_data);
                    }
                    if (bgra != NULL){
                        if (bgra->matte_color)
                            delete bgra->matte_color;
                        DestroyBitmapBgra(bgra);
                        bgra = NULL;
                    }

                }

            };
            public ref class BgraScaler
            {
            public:
                void ScaleBitmap(Bitmap^ source, Bitmap^ dest, Rectangle crop, Rectangle target, array<array<float, 1>^, 1>^ colorMatrix, const InterpolationDetailsPtr details, IProfiler^ p){
                    WrappedBitmap^ bbSource;
                    WrappedBitmap^ bbResult;

                    try{
                        p->Start("SysDrawingToBgra", false);
                        bbSource = WrapBitmapAsBgra(source, crop, !details->allow_source_mutation, true);
                        bbResult = WrapBitmapAsBgra(dest, target, false, true);
                        p->Stop("SysDrawingToBgra", true, false);

                        if (details->use_halving)
                            ScaleBgraWithHalving(bbSource->bgra, target.Width, target.Height, bbResult->bgra, details, p);
                        else
                            ScaleBgra(bbSource->bgra, target.Width, target.Height, bbResult->bgra, details, p);

                        if (colorMatrix != nullptr)
                        {
                            p->Start("ApplyMatrix", false);
                            float *cm[5];
                            for (int i = 0; i < 5; i++)
                            {
                                pin_ptr<float> row = &colorMatrix[i][0];
                                cm[i] = row;
                            }
                            InternalApplyMatrix(bbResult->bgra, cm);
                            p->Stop("ApplyMatrix", false, true);
                        }

                        p->Start("BgraDispose", false);
                    }
                    finally{
                        delete bbSource;
                        delete bbResult;
                        p->Stop("BgraDispose", false, true);
                    }
                }


            private:
                void ScaleBgraWithHalving(BitmapBgraPtr source, int width, int height, BitmapBgraPtr dst, const InterpolationDetailsPtr details, IProfiler^ p){
                    
                    BitmapBgraPtr tmp_im = NULL; 
                    p->Start("ScaleBgraWithHalving", false);
                    try{

                        double divisor_max = MIN((double)source->w / (double)width, 
                                            (double)source->h / (double)height) * 
                                            (1 - details->use_interpolation_for_percent);
                        
                        int divisor = (int)floor(divisor_max);
                        if (details->halve_only_when_common_factor){
                            while (divisor > 0 && ((source->h % divisor != 0) || (source->w % divisor == 0))){
                                divisor--;
                            }
                        }
                        divisor = MAX(1, divisor);
                        int halved_width = source->w / divisor;
                        int halved_height = source->h / divisor;

                        if (divisor > 1){
                            p->Start("Halving", false);

                            if (details->allow_source_mutation && !source->pixels_readonly && !source->stride_readonly){
                                int r = HalveInPlace(source, divisor);
                                if (r == NULL) throw gcnew OutOfMemoryException();
                                p->Stop("Halving", true, false);
                            }
                            else if (halved_width == width && halved_height == height){
                                int r = Halve(source, dst, divisor);
                                if (r == NULL) throw gcnew OutOfMemoryException();
                                p->Stop("Halving", true, false);
                            }else {

                                p->Start("create temp image for halving", false);
                                tmp_im = CreateBitmapBgra(halved_width, halved_height, true, source->bpp);
                                if (tmp_im == NULL) throw gcnew OutOfMemoryException();
                                p->Stop("create temp image for halving", true, false);

                                int r = Halve(source, tmp_im, divisor);
                                if (r == NULL) throw gcnew OutOfMemoryException();

                                p->Stop("Halving", true, false);
                                
                                ScaleBgra(tmp_im, width, height, dst, details, p);
                                return;
                            }
                        }

                        ScaleBgra(source, width, height, dst, details, p);
                    }
                    finally{
                        DestroyBitmapBgra(tmp_im);

                        p->Stop("ScaleBgraWithHalving", true, false);
                    }

                }

                void CopyBgra(BitmapBgraPtr src, BitmapBgraPtr dst)
                {
                    // TODO: check sizes / overflows
                    if (dst->w != src->w || dst->h != src->h)   throw gcnew ArgumentOutOfRangeException();

                    if (src->bpp == 4 && dst->bpp == 4)
                    {
                        // recalculate line width as it can be different from the stride
                        for (int y = 0; y < src->h; y++)
                            memcpy(dst->pixels + y*dst->stride, src->pixels + y*src->stride, src->w*src->bpp);
                    }
                    else if (src->bpp == 3 && dst->bpp == 4)
                    {
                        for (int y = 0; y < src->h; y++)
                            unpack24bitRow(src->w, src->pixels + y*src->stride, dst->pixels + y*dst->stride);
                    }
                    else{
                        throw gcnew NotImplementedException();
                    }
                }

                void ScaleBgra(BitmapBgraPtr source, int width, int height, BitmapBgraPtr dst, const InterpolationDetailsPtr details, IProfiler^ p){
                    if (!dst)throw gcnew ArgumentNullException("dst");

                    if (source->w == width && source->h == height){
                        // TODO: composit on copy
                        // In case of both halfinplace and noresize we still need to copy the data
                        CopyBgra(source, dst);
                        return;
                    }


                    p->Start("ScaleBgra", true);
                    BitmapBgraPtr tmp_im = NULL;

                    // Gamma correction
                    // http://www.4p8.com/eric.brasseur/gamma.html#formulas

                    // Store gamma adjusted in 256-511, linear in 0-255
                    float lut[512];
                    float a = 0.055;
                    
                    for (int n = 0; n < 256; n++)
                    {
                        lut[n] = float(n);
                        float s = n / 255.0;
                        if (s <= 0.04045)
                            lut[256 + n] = s / 12.92;
                        else
                            lut[256 + n] = pow((s + a) / (1 + a), 2.4f);
                    }

                    p->Start("create temp image(sy x dx)", false);
                    /* Scale horizontally  */
                    tmp_im = CreateBitmapBgra(source->h, width, false, source->bpp);
                    if (tmp_im == NULL) { throw gcnew OutOfMemoryException(); }
                    tmp_im->compositing_mode = ::BitmapCompositingMode::Replace_self;

                    try{
                        
                        
                        p->Stop("create temp image(sy x dx)", true, false);

                        p->Start("scale and pivot to temp", false);
                        ScaleXAndPivot(source, tmp_im, details, lut);
                        p->Stop("scale and pivot to temp", true, false);

                        if (details->post_resize_sharpen_percent > 0){
                            p->Start("sharpening along Y axis", false);
                            BgraSharpenInPlaceX(tmp_im, details->post_resize_sharpen_percent);
                            p->Stop("sharpening along Y axis", true, false);
                        }


                        p->Start("scale and pivot to final", false);
                        ScaleXAndPivot(tmp_im, dst, details, lut);
                        p->Stop("scale and pivot to final", true, false);

                        if (details->post_resize_sharpen_percent > 0){
                            p->Start("sharpening along X axis", false);
                            BgraSharpenInPlaceX(dst, details->post_resize_sharpen_percent);
                            p->Stop("sharpening along X axis", true, false);
                        }

                    }
                    finally{
                        p->Start("destroy temp image", false);
                        DestroyBitmapBgra(tmp_im);
                        p->Stop("destroy temp image", true, false);
                        p->Stop("ScaleBgra", true, false);
                    }
                    return;
                }


                WrappedBitmap^ WrapBitmapAsBgra(Bitmap^ source, Rectangle from, bool readonly, bool alpha_meaningful){
                    int i;
                    int j;
                    bool hasAlpha = source->PixelFormat == PixelFormat::Format32bppArgb || source->PixelFormat == PixelFormat::Format32bppPArgb;
                    if (source->PixelFormat != PixelFormat::Format32bppArgb && source->PixelFormat != PixelFormat::Format24bppRgb && source->PixelFormat != PixelFormat::Format32bppPArgb){
                        throw gcnew ArgumentOutOfRangeException("source", "Invalid pixel format " + source->PixelFormat.ToString());
                    }
                    if (from.X < 0 || from.Y < 0 || from.Right > source->Width || from.Bottom > source->Height || from.Width < 1 || from.Height < 1) {
                        throw gcnew ArgumentOutOfRangeException("from");
                    }
                    int sx = from.Width;
                    int sy = from.Height;

                    BitmapBgraPtr im = CreateBitmapBgraHeader(sx, sy);
                    if (im == NULL) throw gcnew InvalidOperationException("Failed to create Bgra Header");

                    WrappedBitmap^ w = gcnew WrappedBitmap(source, im);
                    w->crop_window = from;
                    w->bgra->pixels_readonly = readonly;
                    w->bgra->stride_readonly = true; //We can never mess with the stride when lockbits is used.
                    //LockBits handles cropping for us.
                    w->locked_bitmap_data = source->LockBits(from, readonly ? ImageLockMode::ReadOnly : ImageLockMode::ReadWrite, source->PixelFormat);
                    im->bpp = hasAlpha ? 4 : 3;
                    im->pixels = (unsigned char *)safe_cast<void *>(w->locked_bitmap_data->Scan0);
                    im->stride = w->locked_bitmap_data->Stride;

                    im->w = sx;
                    im->h = sy;

                    im->alpha_meaningful = hasAlpha && alpha_meaningful;
                    
                    
                    return w;
                }
            };
        }
    }
}