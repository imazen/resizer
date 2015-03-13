/*
* Copyright (c) Imazen LLC.
* No part of this project, including this file, may be copied, modified,
* propagated, or distributed except as permitted in COPYRIGHT.txt.
* Licensed under the GNU Affero General Public License, Version 3.0.
* Commercial licenses available at http://imageresizing.net/
*/

#include "shared.h"
#include "weighting.h"
#include "halving.h"
#include "scaling.h"
#include "convolution.h"
#include "sharpening.h"
#include "bitmap_compositing.h"
#include "color_matrix.h"
#pragma once

#ifdef _MSC_VER
#pragma unmanaged
#endif

typedef struct RendererStruct *RendererPtr;


typedef struct RendererStruct {


    RenderDetailsPtr details;
    BitmapBgraPtr source;
    bool destroy_source;
    BitmapBgraPtr canvas;
    BitmapBgraPtr transposed;
    //Todo - profiling callbacks
    //TODO - custom memory pool?
}Renderer;


             

int DetermineDivisor(RendererPtr r) 
{
    if (r->canvas == nullptr) return 0;

    int width = r->details->post_transpose ? r->canvas->h : r->canvas->w;
    int height = r->details->post_transpose ? r->canvas->w : r->canvas->h;


    double divisor_max = MIN((double)r->source->w / (double)width,
        (double)r->source->h / (double)height);

    divisor_max = divisor_max / r->details->interpolate_last_percent;

    int divisor = (int)floor(divisor_max);
    if (r->details->halve_only_when_common_factor) {
        while (divisor > 0 && ((r->source->h % divisor != 0) || (r->source->w % divisor == 0))) {
            divisor--;
        }
    }
    return MAX(1, divisor);
}


void DestroyRenderer(RendererPtr r)
{
    if (r->destroy_source) {
        DestroyBitmapBgra(r->source);
    }
    r->source = nullptr;
    DestroyBitmapBgra(r->transposed);
    r->transposed = nullptr;
    r->canvas = nullptr;
    if (r->details != nullptr) {
        DestroyRenderDetails(r->details);
        r->details = nullptr;
    }
    free(r);              
}

RendererPtr CreateRenderer(BitmapBgraPtr editInPlace, RenderDetailsPtr details)
{
    if (details->post_transpose) return nullptr; //We can't transpose in place. 
    RendererPtr r = (RendererPtr)calloc(1, sizeof(RendererStruct));
    r->source = editInPlace;
    r->destroy_source = false;
    r->details = details;
    return r;
}

RendererPtr CreateRenderer(BitmapBgraPtr source, BitmapBgraPtr canvas, RenderDetailsPtr details)
{
    RendererPtr r = (RendererPtr)calloc(1, sizeof(RendererStruct));
    r->source = source;
    r->canvas = canvas;
    r->destroy_source = false;
    r->details = details;
    return r;
}
    
  
void SimpleRenderInPlace() 
{
    //against source:

    //fliph
    //flipv

    //color matrix (srgb)

}

int CompleteHalving(RendererPtr r)
{
    double divisor = r->details->halving_divisor;
    if (divisor <= 1){
        return 0;
    }
    int halved_width = r->source->w / divisor;
    int halved_height = r->source->h / divisor;

    //p->Start("CompleteHalving", false);
    r->details->halving_divisor = 0; //Don't halve twice

    if (r->source->can_reuse_space){
        int result = HalveInPlace(r->source, divisor);
        if (result == 0) return -101;
    }
    else {
        //p->Start("create temp image for halving", false);
        BitmapBgraPtr tmp_im = CreateBitmapBgra(halved_width, halved_height, true, r->source->bpp);
        if (tmp_im == NULL) return -102;
        //p->Stop("create temp image for halving", true, false);

        int result = Halve(r->source, tmp_im, divisor);
        if (result == 0) {
            return -103;
        }
        tmp_im->alpha_meaningful = r->source->alpha_meaningful;

        if (r->destroy_source) {
            DestroyBitmapBgra(r->source);
        }
        r->source = tmp_im;
        r->destroy_source = true; //Cleanup tmp_im
    }
    return 0;
    // p->Stop("CompleteHalving", true, false);
}


static int ApplyConvolutionsFloat1D(const RendererPtr r, BitmapFloatPtr img, const uint32_t from_row, const uint32_t row_count, double sharpening_applied)
{
    //p->Start("convolve kernel a", false);
    if (r->details->kernel_a_radius > 0 && ConvolveBgraFloatInPlace(img, r->details->kernel_a, r->details->kernel_a_radius, r->details->kernel_a_min, r->details->kernel_a_max, img->channels, from_row, row_count)){
        return -3;
    }
    //p->Stop("convolve kernel a", true, false);
    //p->Start("convolve kernel b", false);
    if (r->details->kernel_b_radius > 0 && ConvolveBgraFloatInPlace(img, r->details->kernel_b, r->details->kernel_b_radius, r->details->kernel_b_min, r->details->kernel_b_max, img->channels, from_row, row_count)){
        return -3;
    }
    //p->Stop("convolve kernel b", true, false);

    if (r->details->sharpen_percent_goal > sharpening_applied + 0.01){
        //p->Start("SharpenBgraFloatRowsInPlace", false);
        SharpenBgraFloatRowsInPlace(img, from_row, row_count, r->details->sharpen_percent_goal - sharpening_applied);
        //p->Stop("SharpenBgraFloatRowsInPlace", true, false);
    }
    return 0;
}

static void ApplyColorMatrix(const RendererPtr r, BitmapFloatPtr img, const uint32_t from_row, const uint32_t row_count)
{
    //p->Start("apply_color_matrix_float", false);
    apply_color_matrix_float(img, 0, row_count, r->details->color_matrix);
    //p->Stop("apply_color_matrix_float", true, false);
}




int ScaleAndRender1D(const RendererPtr r, 
    const BitmapBgraPtr pSrc,
    const BitmapBgraPtr pDst,
    const RenderDetailsPtr details,
    bool transpose,
    int call_number)
{
    LineContribType * contrib = NULL;
    BitmapFloatPtr source_buf = NULL;
    BitmapFloatPtr dest_buf = NULL;

    int return_code = 0;
    uint32_t from_count = pSrc->w;
    uint32_t to_count = transpose ? pDst->h : pDst->w;

    if (details->interpolation->window == 0){
        return -101;
        //throw gcnew ArgumentOutOfRangeException();
    }


    //How many rows to buffer and process at a time.
    const uint32_t buffer_row_count = 4; //using buffer=5 seems about 6% better than most other non-zero values. 

    //How many bytes per pixel are we scaling?
    uint32_t scaling_bpp = (pSrc->bpp == 4 && !pSrc->alpha_meaningful) ? 3 : pSrc->bpp;



    //p->Start("ContributionsCalc", false);
    
    contrib = ContributionsCalc(to_count, from_count, details->interpolation);  /*Handle errors */ if (contrib == NULL) { return_code = -1; goto cleanup; }
    //p->Stop("ContributionsCalc", true, false);


    //p->Start("CreateBitmapFloat (buffers)", false);

    source_buf = CreateBitmapFloat(from_count, buffer_row_count, scaling_bpp, false); /*Handle errors */  if (source_buf == NULL)  { return_code = -1; goto cleanup; }
    
    dest_buf = CreateBitmapFloat(to_count, buffer_row_count, scaling_bpp, false);  /*Handle errors */   if (source_buf == NULL)  { return_code = -1; goto cleanup; }
    source_buf->alpha_meaningful = pSrc->alpha_meaningful;
    dest_buf->alpha_meaningful = source_buf->alpha_meaningful;

    source_buf->alpha_premultiplied = source_buf->channels == 4;
    dest_buf->alpha_premultiplied = source_buf->alpha_premultiplied;

    // p->Stop("CreateBitmapFloat (buffers)", true, false);

    /* Scale each set of lines */
    for (uint32_t source_start_row = 0; source_start_row < pSrc->h; source_start_row += buffer_row_count) {
        const uint32_t row_count = MIN(pSrc->h - source_start_row, buffer_row_count);

        //p->Start("convert_srgb_to_linear", false);
        if (convert_srgb_to_linear(pSrc, source_start_row, source_buf, 0, row_count)){
            return_code = -2; goto cleanup;
        }
        // p->Stop("convert_srgb_to_linear", true, false);

        //p->Start("ScaleBgraFloatRows", false);
        ScaleBgraFloatRows(source_buf, 0, dest_buf, 0, row_count, contrib->ContribRow);
        //p->Stop("ScaleBgraFloatRows", true, false);

        if (ApplyConvolutionsFloat1D(r, dest_buf, 0, row_count, contrib->percent_negative)){
            return_code = -3; goto cleanup;
        }
        if (details->apply_color_matrix && call_number == 2) { ApplyColorMatrix(r, dest_buf, 0, row_count); }

        //p->Start("pivoting_composite_linear_over_srgb", false);
        if (pivoting_composite_linear_over_srgb(dest_buf, 0, pDst, source_start_row, row_count, transpose)){
            return_code = -4; goto cleanup;
        }
        //p->Stop("pivoting_composite_linear_over_srgb", true, false);
    }
    //sRGB sharpening
    //Color matrix


cleanup:
    //p->Start("Free Contributions,FloatBuffers", false);

    if (contrib != NULL) ContributionsFree(contrib);

    if (source_buf != NULL) DestroyBitmapFloat(source_buf);
    if (dest_buf != NULL) DestroyBitmapFloat(dest_buf);
    ///p->Stop("Free Contributions,FloatBuffers", true, false);

    if (return_code != 0){
        // throw gcnew OutOfMemoryException(String::Format("ScaleAndRender1D failed with code {0}", return_code));
    }
    return return_code;
}



int Render1D(const RendererPtr r, 
    const BitmapBgraPtr pSrc,
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
    buf->alpha_meaningful = pSrc->alpha_meaningful;
    buf->alpha_premultiplied = buf->channels == 4;



    /* Scale each set of lines */
    for (uint32_t source_start_row = 0; source_start_row < pSrc->h; source_start_row += buffer_row_count) {
        const uint32_t row_count = MIN(pSrc->h - source_start_row, buffer_row_count);

        if (convert_srgb_to_linear(pSrc, source_start_row, buf, 0, row_count)){
            return_code = -2; goto cleanup;
        }
        if (ApplyConvolutionsFloat1D(r, buf, 0, row_count, 0)){
            return_code = -3; goto cleanup;
        }
        if (details->apply_color_matrix && call_number == 2) { ApplyColorMatrix(r, buf, 0, row_count); }

        if (pivoting_composite_linear_over_srgb(buf, 0, pDst, source_start_row, row_count, transpose)){
            return_code = -4; goto cleanup;
        }
    }
    //sRGB sharpening
    //Color matrix


cleanup:
    if (buf != NULL) DestroyBitmapFloat(buf);
    return return_code;
}


int RenderWrapper1D(
    const RendererPtr r, 
    const BitmapBgraPtr pSrc,
    const BitmapBgraPtr pDst,
    const RenderDetailsPtr details,
    bool transpose,
    int call_number) {
    bool perfect_size = transpose ? (pSrc->h == pDst->w && pDst->h == pSrc->w) : (pSrc->w == pDst->w && pSrc->h == pDst->h);
    //String^ name = String::Format("{0}Render1D (call {1})", perfect_size ? "" : "ScaleAnd", call_number);

    //try{
    // p->Start(name, false);
    if (perfect_size){
        return Render1D(r, pSrc, pDst, details, transpose, call_number);
    }
    else{
        return ScaleAndRender1D(r, pSrc, pDst, details, transpose, call_number);
    }
    // }
    // finally{
    // p->Stop(name, true, true);
    //}
}

int PerformRender(RendererPtr r) {
    CompleteHalving(r);
    bool skip_last_transpose = r->details->post_transpose;

    /*
    //We can optimize certain code paths - later, if needed

    bool scaling_required = (r->canvas != nullptr) && (r->details->post_transpose ? (r->canvas->w != r->source->h || r->canvas->h != r->source->w) :
        (r->canvas->h != r->source->h || r->canvas->w != r->source->w));

    
    bool someTranspositionRequired = r->details->sharpen_percent_goal > 0 ||
        skip_last_transpose ||
        r->details->kernel_a_radius > 0 ||
        r->details->kernel_b_radius > 0 ||
        scaling_required;

    if (!someTranspositionRequired && canvas == nullptr){
        SimpleRenderInPlace(); 
          p->Stop("Render", true, false);
        return; //Nothing left to do here.
    }
    */

    bool vflip_source = (r->details->post_flip_y && !skip_last_transpose) || (skip_last_transpose && r->details->post_flip_x);
    bool vflip_transposed = ((r->details->post_flip_x && !skip_last_transpose) || (skip_last_transpose && r->details->post_flip_y));

    //vertical flip before transposition is the same as a horizontal flip afterwards. Dealing with more pixels, though.
    if (vflip_source && vertical_flip_bgra(r->source)){
        return -1;
    }

    //Create transposition byffer
    //p->Start("allocate temp image(sy x dx)", false);

    /* Scale horizontally  */
    r->transposed = CreateBitmapBgra(r->source->h, r->canvas == nullptr ? r->source->w : (skip_last_transpose ? r->canvas->h : r->canvas->w), false, r->source->bpp);
    if (r->transposed == NULL) { return -2;  }
    r->transposed->compositing_mode = BitmapCompositingMode::Replace_self;
    //p->Stop("allocate temp image(sy x dx)", true, false);

    //Don't composite if we're working in-place
    if (r->canvas == nullptr){
        r->source->compositing_mode = BitmapCompositingMode::Replace_self;
    }
    //Unsharpen when interpolating if we can
    if (r->details->sharpen_percent_goal > 0 &&
        r->details->minimum_sample_window_to_interposharpen <= r->details->interpolation->window){

        r->details->interpolation->sharpen_percent_goal = r->details->sharpen_percent_goal;
    }


    //Apply kernels, scale, and transpose
    if (RenderWrapper1D(r, r->source, r->transposed, r->details, true, 1)){
        return -3;
    }

    //Apply flip to transposed
    if (vflip_transposed && vertical_flip_bgra(r->transposed)){
        return -4;
    }
    //Restore the source bitmap if we flipped it in place incorrectly
    if (vflip_source && r->source->pixels_readonly && vertical_flip_bgra(r->source)){
        return -5;
    }

    BitmapBgraPtr finalDest = r->canvas == nullptr ? r->source : r->canvas;

    //Apply kernels, color matrix, scale,  (transpose?) and (compose?)

    if (RenderWrapper1D(r, r->transposed, finalDest, r->details, !skip_last_transpose, 2)){
        return -6;
    }

    //p->Stop("Render", true, false);
    //GC::KeepAlive(wbSource);
    //GC::KeepAlive(wbCanvas);
}
