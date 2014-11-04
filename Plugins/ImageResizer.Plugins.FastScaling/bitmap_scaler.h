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

            public ref class BgraScaler
            {
            public:
                void ScaleBitmap(Bitmap^ source, Bitmap^ dest, Rectangle crop, Rectangle target, const InterpolationDetailsPtr details, IProfiler^ p){
                    BitmapBgraPtr bbSource;
                    BitmapBgraPtr bbResult;
                    try{
                        p->Start("SysDrawingToBgra", false);
                        bbSource = SysDrawingToBgra(source, crop);
                        bbResult = SysDrawingToBgra(dest, target);
                        p->Stop("SysDrawingToBgra", true, false);

                        if (details->use_halving)
                            ScaleBgraWithHalving(bbSource, target.Width, target.Height, bbResult, details, p);
                        else
                            ScaleBgra(bbSource, target.Width, target.Height, bbResult, details, p);

                        p->Start("BgraDispose", false);
                    }
                    finally{
                        if (bbSource != 0) {
                            DestroyBitmapBgra(bbSource);
                            if (bbResult == bbSource)
                                bbResult = 0;
                            bbSource = 0;
                        }
                        if (bbResult != 0){
                            DestroyBitmapBgra(bbResult);
                            bbResult = 0;
                        }
                        p->Stop("BgraDispose", true, false);

                    }
                }


            private:
                BitmapBgraPtr ScaleBgraWithHalving(BitmapBgraPtr source, int width, int height, BitmapBgraPtr dst, const InterpolationDetailsPtr details, IProfiler^ p){
                    p->Start("ScaleBgraWithHalving", false);
                    try{

                        int divisor = MIN(source->w / width, source->h / height);
                        BitmapBgraPtr tmp_im = 0;

                        if (divisor > 1){
                            p->Start("Halving", false);

                            if (details->allow_source_mutation)
                                HalveInPlace(source, divisor);
                            else
                            {
                                tmp_im = CreateBitmapBgraPtr(source->w / divisor, source->h / divisor, false, 1, source->bpp);
                                if (!tmp_im)
                                    return 0;

                                Halve(source, tmp_im, divisor);

                                p->Stop("Halving", true, false);
                                return ScaleBgra(tmp_im, width, height, dst, details, p);
                            }
                            p->Stop("Halving", true, false);
                        }

                        return ScaleBgra(source, width, height, dst, details, p);
                    }
                    finally{
                        p->Stop("ScaleBgraWithHalving", true, false);
                    }

                }

                void CopyBgra(BitmapBgraPtr src, BitmapBgraPtr dst)
                {
                    // TODO: check sizes / overflows

                    if (src->bpp == 4)
                    {
                        // recalculate line width as it can be different from the stride
                        for (int y = 0; y < src->h; y++)
                            memcpy(dst->pixels + y*dst->stride, src->pixels + y*src->stride, src->w*src->bpp);
                    }
                    else
                    {
                        for (int y = 0; y < src->h; y++)
                            unpack24bitRow(src->w, src->pixels + y*src->stride, dst->pixels + y*dst->stride);
                    }
                }

                BitmapBgraPtr ScaleBgra(BitmapBgraPtr source, int width, int height, BitmapBgraPtr dst, const InterpolationDetailsPtr details, IProfiler^ p){

                    p->Start("create image(dx x dy)", false);
                    if (!dst) dst = CreateBitmapBgraPtr(width, height, false, 1, source->bpp);
                    p->Stop("create image(dx x dy)", true, false);
                    if (dst == NULL) return NULL;


                    if (source->w == width && source->h == height){
                        // In case of both halfinplace and noresize we still need to copy the data
                        CopyBgra(source, dst);
                        return dst;
                    }


                    p->Start("ScaleBgra", true);
                    BitmapBgraPtr tmp_im = NULL;
                    float lut[256];
                    for (int n = 0; n < 256; n++) lut[n] = (float)n;

                    p->Start("create temp image(sy x dx)", false);
                    /* Scale horizontally  */
                    tmp_im = CreateBitmapBgraPtr(source->h, width, false, 1, source->bpp);


                    try{
                        if (tmp_im == NULL) {
                            return NULL;
                        }
                        p->Stop("create temp image(sy x dx)", true, false);

                        p->Start("scale and pivot to temp", false);
                        ScaleXAndPivot(source, tmp_im, details, lut);
                        p->Stop("scale and pivot to temp", true, false);

                        if (details->post_resize_sharpen_percent > 0){
                            p->Start("sharpening along X axis", false);
                            BgraSharpenInPlaceX(tmp_im, details->post_resize_sharpen_percent);
                            p->Stop("sharpening along X axis", true, false);
                        }


                        p->Start("scale and pivot to final", false);
                        ScaleXAndPivot(tmp_im, dst, details, lut);
                        p->Stop("scale and pivot to final", true, false);

                        if (details->post_resize_sharpen_percent > 0){
                            p->Start("sharpening along Y axis", false);
                            BgraSharpenInPlaceX(dst, details->post_resize_sharpen_percent);
                            p->Stop("sharpening along Y axis", true, false);
                        }

                    }
                    finally{
                        p->Start("destroy temp image", false);
                        DestroyBitmapBgra(tmp_im);
                        p->Stop("destroy temp image", true, false);
                        p->Stop("ScaleBgra", true, false);
                    }
                    return dst;
                }

                void BgraToSysDrawing(BitmapBgraPtr source, Bitmap^ target, Rectangle targetArea){
                    if (target->PixelFormat != PixelFormat::Format32bppArgb){
                        throw gcnew ArgumentOutOfRangeException("target", "Invalid pixel format " + target->PixelFormat.ToString());
                    }
                    BitmapData ^targetData;
                    try{
                        targetData = target->LockBits(targetArea, ImageLockMode::ReadOnly, target->PixelFormat);
                        int sy = source->h;
                        int sx = source->w;
                        int i;
                        IntPtr^ scan0intptr = targetData->Scan0;
                        void *scan0 = scan0intptr->ToPointer();
                        for (i = 0; (i < sy); i++) {
                            void * linePtr = (void *)((unsigned long  long)scan0 + (targetData->Stride * (i + targetArea.Top)) + (targetArea.Left * 4));
                            memcpy(linePtr, &source->pixels[i * source->stride], sx * 4);
                        }
                    }
                    finally{
                        target->UnlockBits(targetData);
                    }
                }

                BitmapBgraPtr SysDrawingToBgra(Bitmap^ source, Rectangle from){
                    int i;
                    int j;
                    bool hasAlpha = source->PixelFormat == PixelFormat::Format32bppArgb;
                    if (source->PixelFormat != PixelFormat::Format32bppArgb && source->PixelFormat != PixelFormat::Format24bppRgb){
                        throw gcnew ArgumentOutOfRangeException("source", "Invalid pixel format " + source->PixelFormat.ToString());
                    }
                    if (from.X < 0 || from.Y < 0 || from.Right > source->Width || from.Bottom > source->Height) {
                        throw gcnew ArgumentOutOfRangeException("from");
                    }
                    int sx = from.Width;
                    int sy = from.Height;

                    BitmapBgraPtr im = CreateBitmapBgraPtr(sx, sy, false, false);

                    BitmapData ^sourceData;
                    try{
                        sourceData = source->LockBits(from, ImageLockMode::ReadWrite, source->PixelFormat);

                        IntPtr^ scan0intptr = sourceData->Scan0;

                        void *scan0 = scan0intptr->ToPointer();
                        void *linePtr = (void *)((unsigned long long)scan0 + (unsigned long  long)((sourceData->Stride * from.Top) + (from.Left * (hasAlpha ? 4 : 3))));

                        im->pixels = (unsigned char *)linePtr;
                        im->pixelInts = (unsigned int *)linePtr;
                        im->stride = sourceData->Stride;
                    }
                    finally{
                        source->UnlockBits(sourceData);
                    }
                    im->w = sx;
                    im->h = sy;

                    im->hasAlpha = hasAlpha;
                    if (!hasAlpha)
                        im->bpp = 3;
                    return im;
                }
            };
        }
    }
}