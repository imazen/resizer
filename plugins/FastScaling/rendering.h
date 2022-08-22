/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */

#include "fastscaling.h"
#include "render_options.h"
#pragma once

#ifdef _MSC_VER
#pragma managed
#endif


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
                public ref class ManagedRenderer
                {

                    float* CopyFloatArray (array<float, 1>^ a){
                        if (a == nullptr) return NULL;

                        float * copy = (float *)malloc (sizeof (float) * a->Length);
                        if (copy == NULL) throw gcnew OutOfMemoryException ();
                        for (int i = 0; i < a->Length; i++)
                            copy[i] = a[i];
                        return copy;
                    }

                    ConvolutionKernel* CopyKernel (ConvKernel^ from){
                        if (from == nullptr) return nullptr;
                        ConvolutionKernel* k = ConvolutionKernel_create (c->GetContext (), from->Radius);
                        if (k == nullptr) {
                            throw gcnew FastScalingException (c);
                        }
                        k->threshold_max_change = from->MaxChangeThreshold;
                        k->threshold_min_change = from->MinChangeThreshold;


                        for (uint32_t i = 0; i < Math::Min ((uint32_t)from->Kernel->Length, k->width); i++)
                            k->kernel[i] = (float)from->Kernel[i];
                        return k;
                    }



                    void CopyBasics (RenderOptions^ from, RenderDetails* to){
                        if (from->ColorMatrix != nullptr)
                        {
                            for (int i = 0; i < 5; i++)
                                for (int j = 0; j < 5; j++)
                                    to->color_matrix_data[i * 5 + j] = (float)((array<float, 1>^)from->ColorMatrix->GetValue (i))->GetValue (j);

                            to->apply_color_matrix = true;
                        }
                        to->post_transpose = from->RequiresTransposeStep;
                        to->post_flip_x = from->RequiresHorizontalFlipStep;
                        to->post_flip_y = from->RequiresVerticalFlipStep;
                        to->halving_acceptable_pixel_loss = from->HalvingAcceptablePixelLoss;
                        to->sharpen_percent_goal = from->SharpeningPercentGoal;

                        to->kernel_a = from->KernelA_Struct != nullptr ? from->KernelA_Struct :
                            from->KernelA != nullptr ? CopyKernel (from->KernelA) : nullptr;
                        to->kernel_b = from->KernelB_Struct != nullptr ? from->KernelB_Struct :
                            from->KernelB != nullptr ? CopyKernel (from->KernelB) : nullptr;

                        to->interpolate_last_percent = (float)from->InterpolateLastPercent;


                        if (to->interpolation != nullptr){
                            InterpolationDetails_destroy (c->GetContext (), to->interpolation);
                            to->interpolation = nullptr;
                        }

                        to->interpolation = InterpolationDetails_create_from (c->GetContext (), (::InterpolationFilter)from->Filter);
                        if (to->interpolation == nullptr) {
                            throw gcnew FastScalingException (c);
                        }
                        to->interpolation->blur *= from->SamplingBlurFactor;
                        if (from->SamplingWindowOverride != 0) {
                            to->interpolation->window = from->SamplingWindowOverride;
                        }
                        to->minimum_sample_window_to_interposharpen = from->MinSamplingWindowToIntegrateSharpening;


                    }


                    ~ManagedRenderer (){
                        if (p != nullptr) p->Start ("Renderer: dispose", false);
                        RenderDetails* temp = details;
                        details = NULL;
                        RenderDetails_destroy (c->GetContext (), temp);


                        if (wbSource != nullptr){
                            delete wbSource;
                            wbSource = nullptr;
                        }
                        if (wbCanvas != nullptr){
                            delete wbCanvas;
                            wbCanvas = nullptr;
                        }

                        if (p != nullptr) p->Stop ("Renderer: dispose", true, false);
                    }
                    RenderDetails* details;
                    RenderOptions^ originalOptions;
                    WrappedBitmap^ wbSource;
                    WrappedBitmap^ wbCanvas;
                    IProfiler^ p;
                    ExecutionContext^ c;

                public:

                    BitmapBgra * source_bgra (){
                        return wbSource->bgra;
                    }

                    ManagedRenderer (ExecutionContext^ c, BitmapOptions^ editInPlace, RenderOptions^ opts, IProfiler^ p){
                        this->p = p;
                        this->c = c;
                        originalOptions = opts;


                        if (opts->RequiresTransposeStep) throw gcnew ArgumentException ("Cannot transpose image in place.");

                        details = RenderDetails_create (c->GetContext ());
                        if (details == NULL){
                            throw gcnew FastScalingException (c);
                        }
                        CopyBasics (opts, details);
                        if (p != nullptr) p->Start ("SysDrawingToBgra", false);
                        wbSource = gcnew WrappedBitmap (c, editInPlace);
                        if (p != nullptr) p->Stop ("SysDrawingToBgra", true, false);

                    }

                    ManagedRenderer (ExecutionContext^ c, BitmapOptions^ source, BitmapOptions^ canvas, RenderOptions^ opts, IProfiler^ p){

                        this->p = p;
                        this->c = c;
                        originalOptions = opts;

                        details = RenderDetails_create (c->GetContext ());
                        if (details == nullptr){
                            throw gcnew FastScalingException (c);
                        }
                        details->enable_profiling = p != nullptr && p->Active;
                        CopyBasics (opts, details);
                        if (p != nullptr) p->Start ("SysDrawingToBgra", false);
                        wbSource = gcnew WrappedBitmap (c, source);
                        wbCanvas = gcnew WrappedBitmap (c, canvas);
                        if (p != nullptr) p->Stop ("SysDrawingToBgra", true, false);

                    }

                    // May adjust the colorspace of the context
                    void Render (){

                        if (p != nullptr) p->Start ("managed_perform_render", false);
                        if (p != nullptr) p->Start ("set_colorspace", false);

                        c->UseFloatspace (originalOptions->ScalingColorspace, originalOptions->ColorspaceParamA, originalOptions->ColorspaceParamB, originalOptions->ColorspaceParamC);


                        if (p != nullptr) p->Stop ("set_colorspace", true, true);

                        bool result = false;
                        if (wbCanvas == nullptr){
                            result = RenderDetails_render_in_place (c->GetContext (), details, wbSource->bgra);
                        }
                        else{
                            result = RenderDetails_render (c->GetContext (), details, wbSource->bgra, wbCanvas->bgra);
                        }
                        if (!result){
                            throw gcnew FastScalingException (c);

                        }
                        replay_log ();
                        if (p != nullptr) p->Stop ("managed_perform_render", true, true);

                    }

                private:
                    void replay_log (){
                        if (p == nullptr) return;
                        ProfilingLog * log = Context_get_profiler_log (c->GetContext ());
                        if (log == nullptr || log->capacity == 0) return;
                        if (log->count >= log->capacity) throw gcnew FastScalingException ("Profiling log was not large enough to contain all messages");
                        for (uint32_t i = 0; i < log->count; i++){
                            ProfilingEntry entry = log->log[i];
                            bool start = (entry.flags & ::ProfilingEntryFlags::Profiling_start) > 0;
                            bool allowRecursion = (entry.flags & ::ProfilingEntryFlags::Profiling_start_allow_recursion) > 0;
                            bool stop = (entry.flags & ::ProfilingEntryFlags::Profiling_stop) > 0;
                            bool assert_started = (entry.flags & ::ProfilingEntryFlags::Profiling_stop_assert_started) > 0;
                            bool stop_children = (entry.flags & ::ProfilingEntryFlags::Profiling_stop_children) > 0;

                            if (start){
                                p->LogStart (entry.time, gcnew String (entry.name), allowRecursion);
                            }
                            else if (stop){
                                p->LogStop (entry.time, gcnew String (entry.name), assert_started, stop_children);
                            }

                        }
                    }

                };

            }
        }
    }
}
