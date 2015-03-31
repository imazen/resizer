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
using namespace ImageResizer::Resizing;
using namespace System::Diagnostics;
using namespace System::Collections::Specialized;
using namespace System::Runtime::InteropServices;

namespace ImageResizer{
    namespace Plugins{
        namespace FastScaling {

            public ref class ManagedRenderer
            {

                float* CopyFloatArray(array<float, 1>^ a){
                    if (a == nullptr) return NULL;

                    float * copy = (float *)malloc(sizeof(float) * a->Length);
                    if (copy == NULL) throw gcnew OutOfMemoryException();
                    for (int i = 0; i < a->Length; i++)
                        copy[i] = a[i];
                    return copy;
                }

                ConvolutionKernel* CopyKernel (ConvKernel^ from){
                    if (from == nullptr) return nullptr;
                    ConvolutionKernel* k = ConvolutionKernel_create (c->GetContext(), from->Radius);
                    if (k == nullptr) return nullptr;
                    k->threshold_max_change = from->MaxChangeThreshold;
                    k->threshold_min_change = from->MinChangeThreshold;


                    for (int i = 0; i < Math::Min((uint32_t)from->Kernel->Length, k->width); i++)
                        k->kernel[i] = (float)from->Kernel[i];
                    return k;
                }



                void CopyBasics(RenderOptions^ from, RenderDetails* to){
                    if (from->ColorMatrix != nullptr)
                    {
                        for (int i = 0; i < 5; i++)
                            for (int j = 0; j < 5; j++)
                                to->color_matrix_data[i * 5 + j] = (float)((array<float,1>^)from->ColorMatrix->GetValue(i))->GetValue(j);

                        to->apply_color_matrix = true;
                    }
                    to->post_transpose = from->RequiresTransposeStep;
                    to->post_flip_x = from->RequiresHorizontalFlipStep;
                    to->post_flip_y = from->RequiresVerticalFlipStep;
                    to->halve_only_when_common_factor = from->HalveOnlyWhenPerfect;
                    to->sharpen_percent_goal = from->SharpeningPercentGoal;

                    to->kernel_a = from->KernelA_Struct != nullptr ? from->KernelA_Struct :
                        from->KernelA != nullptr ? CopyKernel (from->KernelA) : nullptr;
                    to->kernel_b = from->KernelB_Struct != nullptr ? from->KernelB_Struct :
                        from->KernelB != nullptr ? CopyKernel (from->KernelB) : nullptr;

                    to->interpolate_last_percent = from->InterpolateLastPercent;


                    if (to->interpolation != nullptr){
                        free(to->interpolation);
                        to->interpolation = nullptr;
                    }

                    to->interpolation = InterpolationDetails_create_from (c->GetContext (),from->Filter);
                    if (to->interpolation == nullptr) throw gcnew ArgumentOutOfRangeException("Invalid filter value");
                    to->interpolation->blur *= from->SamplingBlurFactor;
                    if (from->SamplingWindowOverride != 0) {
                        to->interpolation->window = from->SamplingWindowOverride;
                    }
                    to->minimum_sample_window_to_interposharpen = from->MinSamplingWindowToIntegrateSharpening;


                }


                ~ManagedRenderer(){
                    if (p != nullptr) p->Start("Renderer: dispose", false);
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

                    if (p != nullptr) p->Stop("Renderer: dispose", true, false);
                }
                RenderDetails* details;
                RenderOptions^ originalOptions;
                WrappedBitmap^ wbSource;
                WrappedBitmap^ wbCanvas;
                IProfiler^ p;
                ExecutionContext^ c;

            public:


                ManagedRenderer (ExecutionContext^ c, BitmapOptions^ editInPlace, RenderOptions^ opts, IProfiler^ p){
                    this->p = p;
                    this->c = c;
                    originalOptions = opts;


                    if (opts->RequiresTransposeStep) throw gcnew ArgumentException("Cannot transpose image in place.");

                    details = RenderDetails_create (c->GetContext());
                    CopyBasics(opts, details);
                    p->Start("SysDrawingToBgra", false);
                    wbSource = gcnew WrappedBitmap(c,editInPlace);
                    p->Stop("SysDrawingToBgra", true, false);

                }

                ManagedRenderer(ExecutionContext^ c, BitmapOptions^ source, BitmapOptions^ canvas, RenderOptions^ opts, IProfiler^ p){

                    this->p = p;
                    this->c = c;
                    originalOptions = opts;

                    details = RenderDetails_create(c->GetContext());
                    details->enable_profiling = p->Active;
                    CopyBasics(opts, details);
                    p->Start("SysDrawingToBgra", false);
                    wbSource = gcnew WrappedBitmap(c, source);
                    wbCanvas = gcnew WrappedBitmap(c, canvas);
                    p->Stop("SysDrawingToBgra", true, false);

                }


                void Render(){
                    p->Start ("managed_perform_render", false);

                    bool result = false;
                    if (wbCanvas == nullptr){
                        result = RenderDetails_render_in_place (c->GetContext (), details, wbSource->bgra);
                    }
                    else{
                        result = RenderDetails_render (c->GetContext (), details, wbSource->bgra, wbCanvas->bgra);
                    }
                    if (!result){
                       char buffer[2048];

                        FastScalingResult result_code = (FastScalingResult)Context_error_reason (this->c->GetContext ());
                        String^ message = result_code.ToString () + gcnew String ("\n") + gcnew String (Context_stacktrace (c->GetContext (), buffer, 2047));

                        //TODO: refactor and ensure call stack and status code are returned.
                        if (result_code == FastScalingResult::Out_of_memory)
                            throw gcnew OutOfMemoryException (message);
                        else
                            throw gcnew Exception (message);

                    }
                    replay_log ();
                    p->Stop ("managed_perform_render", true, true);

                }

                private:
                    void replay_log (){
                        ProfilingLog * log = Context_get_profiler_log (c->GetContext ());
                        if (log == nullptr || log->capacity == 0) return;
                        if (log->count >= log->capacity) throw gcnew OutOfMemoryException ("Profiling log was not large enough to contain all messages");
                        for (int i = 0; i < log->count; i++){
                            ProfilingEntry entry = log->log[i];
                            bool start = (entry.flags & ProfilingEntryFlags::Profiling_start) > 0;
                            bool allowRecursion = (entry.flags & ProfilingEntryFlags::Profiling_start_allow_recursion) > 0;
                            bool stop = (entry.flags & ProfilingEntryFlags::Profiling_stop) > 0;
                            bool assert_started =  (entry.flags & ProfilingEntryFlags::Profiling_stop_assert_started) > 0;
                            bool stop_children =  (entry.flags & ProfilingEntryFlags::Profiling_stop_children) > 0;

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
