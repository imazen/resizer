#include "Stdafx.h"
#include "shared.h"

#include "bitmap_compositing.h"
#include "ImageResizer.Plugins.FastScaling.h"
#include "managed_bitmap_wrapper.h"
#include "color_matrix.h"
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

            public ref class Renderer
            {

                float* CopyFloatArray(array<float, 1>^ a){
                    if (a == nullptr) return NULL;

                    float * copy = (float *)ir_malloc(sizeof(float) * a->Length);
                    if (copy == NULL) throw gcnew OutOfMemoryException();
                    for (int i = 0; i < a->Length; i++)
                        copy[i] = a[i];
                    return copy;
                }

                void CopyBasics(RenderOptions^ from, RenderDetailsPtr to){
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
                    to->kernel_a_min = from->ConvolutionA_MinChangeThreshold;
                    to->kernel_a_max = from->ConvolutionA_MaxChangeThreshold;
                    to->kernel_b_min = from->ConvolutionB_MinChangeThreshold;
                    to->kernel_b_max = from->ConvolutionB_MaxChangeThreshold;
                    to->kernel_a = CopyFloatArray(from->ConvolutionA);
                    to->kernel_b = CopyFloatArray(from->ConvolutionB);
                    to->interpolate_last_percent = from->InterpolateLastPercent;


                    if (to->interpolation != nullptr){
                        free(to->interpolation);
                        to->interpolation = nullptr;
                    }

                    to->interpolation = CreateInterpolation(from->Filter);
                    if (to->interpolation == nullptr) throw gcnew ArgumentOutOfRangeException("Invalid filter value");
                    to->interpolation->blur *= from->SamplingBlurFactor;
                    if (from->SamplingWindowOverride != 0) {
                        to->interpolation->window = from->SamplingWindowOverride;
                    }
                    to->minimum_sample_window_to_interposharpen = from->MinSamplingWindowToIntegrateSharpening;


                }

                void CopyBgra(BitmapBgraPtr src, BitmapBgraPtr dst)
                {
                    int result = copy_bitmap_bgra(src, dst);
                    // TODO: check sizes / overflows
                    if (result == -1)   throw gcnew ArgumentOutOfRangeException();
                    else if (result != 0) throw gcnew NotImplementedException();

                }
                int DetermineDivisor(){
                    if (canvas == nullptr) return 0;

                    int width = details->post_transpose ? canvas->h : canvas->w;
                    int height = details->post_transpose ? canvas->w : canvas->h;


                    double divisor_max = MIN((double)source->w / (double)width,
                        (double)source->h / (double)height);

                    divisor_max = divisor_max / details->interpolate_last_percent;

                    int divisor = (int)floor(divisor_max);
                    if (details->halve_only_when_common_factor){
                        while (divisor > 0 && ((source->h % divisor != 0) || (source->w % divisor == 0))){
                            divisor--;
                        }
                    }
                    return MAX(1, divisor);
                }
                ~Renderer(){
                    if (p != nullptr) p->Start("Renderer: dispose", false);
                    if (free_source && this->source != nullptr){
                        DestroyBitmapBgra(this->source);
                    }
                    source = nullptr;
                    canvas = nullptr;
                    
                    if (wbSource != nullptr){
                        delete wbSource;
                        wbSource = nullptr;
                    }
                    if (wbCanvas != nullptr){
                        delete wbCanvas;
                        wbCanvas = nullptr;
                    }
                    if (this->details != NULL){
                        DestroyRenderDetails(this->details);
                        this->details = NULL;
                    }
                    if (transposed != nullptr){
                        DestroyBitmapBgra(transposed);
                        transposed = nullptr;
                    }
                    if (p != nullptr) p->Stop("Renderer: dispose", true, false);
                }
                RenderDetailsPtr details;
                RenderOptions^ originalOptions;
                WrappedBitmap^ wbSource;
                WrappedBitmap^ wbCanvas;
                bool free_source;
                BitmapBgraPtr source;
                BitmapBgraPtr canvas;
                BitmapBgraPtr transposed;
                IProfiler^ p;

            public:


                Renderer(BitmapOptions^ editInPlace, RenderOptions^ opts, IProfiler^ p){
                    this->p = p;
                    originalOptions = opts;
                    free_source = false;

                    if (opts->RequiresTransposeStep) throw gcnew ArgumentException("Cannot transpose image in place.");

                    details = CreateRenderDetails();
                    CopyBasics(opts, details);
                    p->Start("SysDrawingToBgra", false);
                    wbSource = gcnew WrappedBitmap(editInPlace);
                    p->Stop("SysDrawingToBgra", true, false);
                }

                Renderer(BitmapOptions^ source, BitmapOptions^ canvas, RenderOptions^ opts, IProfiler^ p){

                    this->p = p;
                    free_source = false;
                    originalOptions = opts;
                    details = CreateRenderDetails();
                    CopyBasics(opts, details);
                    p->Start("SysDrawingToBgra", false);
                    wbSource = gcnew WrappedBitmap(source);
                    wbCanvas = gcnew WrappedBitmap(canvas);
                    this->source = wbSource->bgra;
                    this->canvas = wbCanvas->bgra;
                    p->Stop("SysDrawingToBgra", true, false);

                    details->halving_divisor = DetermineDivisor();
                }


                void Render(){
                    p->Start("Render", false);
                    CompleteHalving();

                    bool skip_last_transpose = details->post_transpose;

                    bool scaling_required = (canvas != nullptr) && (details->post_transpose ? (canvas->w != source->h || canvas->h != source->w) :
                        (canvas->h != source->h || canvas->w != source->w));

                    bool someTranspositionRequired = details->sharpen_percent_goal > 0 ||
                        skip_last_transpose ||
                        details->kernel_a_radius > 0 ||
                        details->kernel_b_radius > 0 ||
                        scaling_required;

                    //We can optimize certain code paths - later, if needed.
                    //if (!someTranspositionRequired && canvas == nullptr){
                    //    SimpleRenderInPlace(); 
                    //      p->Stop("Render", true, false);
                    //    return; //Nothing left to do here.
                    //}

                    bool vflip_source = (details->post_flip_y && !skip_last_transpose) || (skip_last_transpose && details->post_flip_x);
                    bool vflip_transposed = ((details->post_flip_x && !skip_last_transpose) || (skip_last_transpose && details->post_flip_y));

                    //vertical flip before transposition is the same as a horizontal flip afterwards. Dealing with more pixels, though.
                    if (vflip_source && vertical_flip_bgra(source)){
                        throw gcnew OutOfMemoryException();
                    }


                    //Create transposition byffer
                    p->Start("allocate temp image(sy x dx)", false);

                    /* Scale horizontally  */
                    transposed = CreateBitmapBgra(source->h, canvas == nullptr ? source->w : (skip_last_transpose ? canvas->h : canvas->w), false, source->bpp);
                    if (transposed == NULL) { throw gcnew OutOfMemoryException(); }
                    transposed->compositing_mode = ::BitmapCompositingMode::Replace_self;
                    p->Stop("allocate temp image(sy x dx)", true, false);

                    //Don't composite if we're working in-place
                    if (canvas == nullptr){
                        source->compositing_mode = ::BitmapCompositingMode::Replace_self;
                    }
                    //Unsharpen when interpolating if we can
                    if (details->sharpen_percent_goal > 0 &&
                        details->minimum_sample_window_to_interposharpen <= details->interpolation->window){

                        details->interpolation->sharpen_percent_goal = details->sharpen_percent_goal;
                    }


                    //Apply kernels, scale, and transpose
                    if (RenderWrapper1D(source, transposed, details, true, 1)){
                        throw gcnew OutOfMemoryException();
                    }

                    //Apply flip to transposed
                    if (vflip_transposed && vertical_flip_bgra(transposed)){
                        throw gcnew OutOfMemoryException();
                    }
                    //Restore the source bitmap if we flipped it in place incorrectly
                    if (vflip_source && source->pixels_readonly && vertical_flip_bgra(source)){
                        throw gcnew OutOfMemoryException();
                    }

                    BitmapBgraPtr finalDest = canvas == nullptr ? source : canvas;

                    //Apply kernels, color matrix, scale,  (transpose?) and (compose?)

                    if (RenderWrapper1D(transposed, finalDest, details, !skip_last_transpose, 2)){
                        throw gcnew OutOfMemoryException();
                    }

                    p->Stop("Render", true, false); 
                    GC::KeepAlive(wbSource);
                    GC::KeepAlive(wbCanvas);
                }
                void SimpleRenderInPlace(){
                    //against source:

                    //fliph
                    //flipv

                    //color matrix (srgb)

                }

            private:
                void CompleteHalving(){
                    double divisor = details->halving_divisor;
                    if (divisor <= 1){
                        return;
                    }
                    int halved_width = source->w / divisor;
                    int halved_height = source->h / divisor;

                    p->Start("CompleteHalving", false);
                    details->halving_divisor = 0; //Don't halve twice

                    if (source->can_reuse_space){
                        int r = HalveInPlace(source, divisor);
                        if (r == NULL) throw gcnew OutOfMemoryException();
                    }
                    else {

                        p->Start("create temp image for halving", false);
                        BitmapBgraPtr tmp_im = CreateBitmapBgra(halved_width, halved_height, true, source->bpp);
                        if (tmp_im == NULL) throw gcnew OutOfMemoryException();
                        p->Stop("create temp image for halving", true, false);

                        int r = Halve(source, tmp_im, divisor);
                        if (r == NULL) throw gcnew OutOfMemoryException();

                        this->source = nullptr;
                        //We no longer need/want wbSource and source
                        delete wbSource;
                        wbSource = nullptr;
                        this->source = tmp_im;
                        free_source = true; //Cleanup tmp_im

                    }


                    p->Stop("CompleteHalving", true, false);
                }


                static int ApplyConvolutionsFloat1D(BitmapFloatPtr img, const uint32_t from_row, const uint32_t row_count, double sharpening_applied, const RenderDetailsPtr details, IProfiler^ p){
                    p->Start("convolve kernel a", false);
                    if (details->kernel_a_radius > 0 && ConvolveBgraFloatInPlace(img, details->kernel_a, details->kernel_a_radius, details->kernel_a_min, details->kernel_a_max, img->channels, from_row, row_count)){
                        return -3;
                    }
                    p->Stop("convolve kernel a", true, false);
                    p->Start("convolve kernel b", false);
                    if (details->kernel_b_radius > 0 && ConvolveBgraFloatInPlace(img, details->kernel_b, details->kernel_b_radius, details->kernel_b_min, details->kernel_b_max, img->channels, from_row, row_count)){
                        return -3;
                    }
                    p->Stop("convolve kernel b", true, false);

                    if (details->sharpen_percent_goal > sharpening_applied + 0.01 ){
                        p->Start("SharpenBgraFloatRowsInPlace", false);
                        SharpenBgraFloatRowsInPlace(img, from_row, row_count, details->sharpen_percent_goal - sharpening_applied);
                        p->Stop("SharpenBgraFloatRowsInPlace", true, false);
                    }
                    return 0;
                }

                static void ApplyColorMatrix(BitmapFloatPtr img, const uint32_t from_row, const uint32_t row_count, const RenderDetailsPtr details, IProfiler^ p){
                    p->Start("apply_color_matrix_float", false);
                    apply_color_matrix_float(img, 0, row_count, details->color_matrix);
                    p->Stop("apply_color_matrix_float", true, false);
                }

                int RenderWrapper1D(const BitmapBgraPtr pSrc,
                    const BitmapBgraPtr pDst,
                    const RenderDetailsPtr details,
                    bool transpose,
                    int call_number){
                    bool perfect_size = transpose ? (pSrc->h == pDst->w && pDst->h == pSrc->w) : (pSrc->w == pDst->w && pSrc->h == pDst->h);
                    String^ name = String::Format("{0}Render1D (call {1})", perfect_size ? "" : "ScaleAnd", call_number);

                    try{
                        p->Start(name, false);
                        if (perfect_size){
                            return Render1D(pSrc, pDst, details, transpose, call_number);
                        }
                        else{
                            return ScaleAndRender1D(pSrc, pDst, details, transpose, call_number);
                        }
                    }
                    finally{
                        p->Stop(name, true, true);
                    }

                }


                int ScaleAndRender1D(const BitmapBgraPtr pSrc,
                    const BitmapBgraPtr pDst,
                    const RenderDetailsPtr details,
                    bool transpose,
                    int call_number)
                {

                    int return_code = 0;
                    uint32_t from_count = pSrc->w;
                    uint32_t to_count = transpose ? pDst->h : pDst->w;

                    if (details->interpolation->window == 0){
                        throw gcnew ArgumentOutOfRangeException();
                    }

                    p->Start("ContributionsCalc", false);
                    LineContribType * contrib = ContributionsCalc(to_count, from_count, details->interpolation);  /*Handle errors */ if (contrib == NULL) { return_code = -1; goto cleanup; }
                    p->Stop("ContributionsCalc", true, false);


                    //How many rows to buffer and process at a time.
                    const uint32_t buffer_row_count = 4; //using buffer=5 seems about 6% better than most other non-zero values. 

                    //How many bytes per pixel are we scaling?
                    uint32_t scaling_bpp = (pSrc->bpp == 4 && !pSrc->alpha_meaningful) ? 3 : pSrc->bpp;

                    p->Start("CreateBitmapFloat (buffers)", false);
                    BitmapFloatPtr source_buf = CreateBitmapFloat(from_count, buffer_row_count, scaling_bpp, false); /*Handle errors */  if (source_buf == NULL)  { return_code = -1; goto cleanup; }
                    BitmapFloatPtr dest_buf = CreateBitmapFloat(to_count, buffer_row_count, scaling_bpp, false);  /*Handle errors */   if (source_buf == NULL)  { return_code = -1; goto cleanup; }
                    source_buf->alpha_meaningful = pSrc->alpha_meaningful;
                    dest_buf->alpha_meaningful = source_buf->alpha_meaningful;

                    p->Stop("CreateBitmapFloat (buffers)", true, false);

                    /* Scale each set of lines */
                    for (uint32_t source_start_row = 0; source_start_row < pSrc->h; source_start_row += buffer_row_count) {
                        const uint32_t row_count = MIN(pSrc->h - source_start_row, buffer_row_count);

                        p->Start("convert_srgb_to_linear", false);
                        if (convert_srgb_to_linear(pSrc, source_start_row, source_buf, 0, row_count)){
                            return_code = -2; goto cleanup;
                        }
                        p->Stop("convert_srgb_to_linear", true, false);

                        p->Start("ScaleBgraFloatRows", false);
                        ScaleBgraFloatRows(source_buf, 0, dest_buf, 0, row_count, contrib->ContribRow);
                        p->Stop("ScaleBgraFloatRows", true, false);

                        if (ApplyConvolutionsFloat1D(dest_buf, 0, row_count, contrib->percent_negative,details,    p)){
                            return_code = -3; goto cleanup;
                        }
                        if (details->apply_color_matrix && call_number == 2) { ApplyColorMatrix(dest_buf, 0, row_count, details, p); }

                        p->Start("pivoting_composite_linear_over_srgb", false);
                        if (pivoting_composite_linear_over_srgb(dest_buf, 0, pDst, source_start_row, row_count,transpose)){
                            return_code = -4; goto cleanup;
                        }
                        p->Stop("pivoting_composite_linear_over_srgb", true, false);
                    }
                    //sRGB sharpening
                    //Color matrix


                cleanup:
                    p->Start("Free Contributions,FloatBuffers", false);
                    
                    if (contrib != NULL) ContributionsFree(contrib);
                    
                    if (source_buf != NULL) DestroyBitmapFloat(source_buf);
                    if (dest_buf != NULL) DestroyBitmapFloat(dest_buf);
                    p->Stop("Free Contributions,FloatBuffers", true,false);

                    if (return_code != 0){
                        throw gcnew OutOfMemoryException(String::Format("ScaleAndRender1D failed with code {0}", return_code));
                    }
                    return return_code;
                }



                int Render1D(const BitmapBgraPtr pSrc,
                    const BitmapBgraPtr pDst,
                    const RenderDetailsPtr details,
                    bool transpose,
                    int call_number)
                {

                    int return_code = 0;

                    //How many rows to buffer and process at a time.
                    uint32_t buffer_row_count = 4; //using buffer=5 seems about 6% better than most other non-zero values. 

                    //How many bytes per pixel are we scaling?
                    uint32_t scaling_bpp = (pSrc->bpp == 4 && !pSrc->alpha_meaningful) ? 3 : pSrc->bpp;

                    BitmapFloatPtr buf = CreateBitmapFloat(pSrc->w, buffer_row_count, scaling_bpp, false); /*Handle errors */  if (buf == NULL)  { return_code = -1; goto cleanup; }

                    /* Scale each set of lines */
                    for (uint32_t source_start_row = 0; source_start_row < pSrc->h; source_start_row += buffer_row_count) {
                        const uint32_t row_count = MIN(pSrc->h - source_start_row, buffer_row_count);

                        if (convert_srgb_to_linear(pSrc, source_start_row, buf, 0, row_count)){
                            return_code = -2; goto cleanup;
                        }
                        if (Renderer::ApplyConvolutionsFloat1D(buf, 0, row_count, 0, details, this->p)){
                            return_code = -3; goto cleanup;
                        }
                        if (details->apply_color_matrix && call_number == 2) { ApplyColorMatrix(buf, 0, row_count, details,p); }

                        if (pivoting_composite_linear_over_srgb(buf, 0, pDst, source_start_row, row_count,transpose)){
                            return_code = -4; goto cleanup;
                        }
                    }
                    //sRGB sharpening
                    //Color matrix


                cleanup:
                    if (buf != NULL) DestroyBitmapFloat(buf);
                    return return_code;
                }

            };

        }
    }
}