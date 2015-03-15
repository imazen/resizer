/*
* Copyright (c) Imazen LLC.
* No part of this project, including this file, may be copied, modified,
* propagated, or distributed except as permitted in COPYRIGHT.txt.
* Licensed under the GNU Affero General Public License, Version 3.0.
* Commercial licenses available at http://imageresizing.net/
*/

#ifdef _MSC_VER
#pragma unmanaged
#endif


#include "shared.h"
#include "weighting.h"
#include "halving.h"
#include "scaling.h"
#include "convolution.h"
#include "sharpening.h"
#include "bitmap_compositing.h"
#include "fastscaling.h"



typedef struct RendererStruct {
    RenderDetails * details;
    BitmapBgra * source;
    bool destroy_source;
    BitmapBgra * canvas;
    BitmapBgra * transposed;
    //Todo - profiling callbacks
    //TODO - custom memory pool?
} Renderer;


InterpolationDetails* CreateInterpolationDetails()
{
    InterpolationDetails* d = (InterpolationDetails*)calloc(1, sizeof(InterpolationDetails));
    d->blur = 1;
    d->window = 2;
    d->p1 = d->q1 = 0;
    d->p2 = d->q2 = d->p3 = d->q3 = d->q4 = 1;
    d->sharpen_percent_goal = 0;
    return d;
}

RenderDetails * CreateRenderDetails()
{
    RenderDetails * d = (RenderDetails *)calloc(1, sizeof(RenderDetails));
    for (int i = 0; i < 5; i++) {
        d->color_matrix[i] = &(d->color_matrix_data[i * 5]);
    }
    d->interpolate_last_percent = 3;
    d->halve_only_when_common_factor = true;
    d->minimum_sample_window_to_interposharpen = 1.5;
    d->apply_color_matrix = false;
    return d;
}

static void DestroyRenderDetails(RenderDetails * d){
    if (d->interpolation != NULL) free(d->interpolation);

    if (d->kernel_a != NULL) ir_free(d->kernel_a);

    if (d->kernel_b != NULL) ir_free(d->kernel_b);

    free(d);
}


             

static int DetermineDivisor(Renderer * r) 
{
    if (r->canvas == NULL) return 0;

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


void DestroyRenderer(Renderer * r)
{
    if (r->destroy_source) {
        DestroyBitmapBgra(r->source);
    }
    r->source = NULL;
    DestroyBitmapBgra(r->transposed);
    r->transposed = NULL;
    r->canvas = NULL;
    if (r->details != NULL) {
        DestroyRenderDetails(r->details);
        r->details = NULL;
    }
    free(r);              
}

static Renderer * CreateRendererInPlace(BitmapBgra * editInPlace, RenderDetails * details)
{
    if (details->post_transpose) return NULL; //We can't transpose in place. 
    Renderer * r = (Renderer *)calloc(1, sizeof(Renderer));
    r->source = editInPlace;
    r->destroy_source = false;
    r->details = details;
    return r;
}

Renderer * CreateRenderer(BitmapBgra * source, BitmapBgra * canvas, RenderDetails * details)
{
    Renderer * r = (Renderer *)calloc(1, sizeof(Renderer));
    r->source = source;
    r->canvas = canvas;
    r->destroy_source = false;
    r->details = details;
    return r;
}
    
  
static void SimpleRenderInPlace(void) 
{
    //against source:

    //fliph
    //flipv

    //color matrix (srgb)

}

static int CompleteHalving(Renderer * r)
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
        BitmapBgra * tmp_im = CreateBitmapBgra(halved_width, halved_height, true, r->source->bpp);
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


static int ApplyConvolutionsFloat1D(const Renderer * r, BitmapFloat * img, const uint32_t from_row, const uint32_t row_count, double sharpening_applied)
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

static void ApplyColorMatrix(const Renderer * r, BitmapFloat * img, const uint32_t row_count)
{
    //p->Start("apply_color_matrix_float", false);
    apply_color_matrix_float(img, 0, row_count, r->details->color_matrix);
    //p->Stop("apply_color_matrix_float", true, false);
}




static int ScaleAndRender1D(const Renderer * r, 
    BitmapBgra * pSrc,
    BitmapBgra * pDst,
    const RenderDetails * details,
    bool transpose,
    int call_number)
{
    LineContribType * contrib = NULL;
    BitmapFloat * source_buf = NULL;
    BitmapFloat * dest_buf = NULL;

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
        if (details->apply_color_matrix && call_number == 2) { ApplyColorMatrix(r, dest_buf, row_count); }

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



static int Render1D(const Renderer * r, 
    BitmapBgra * pSrc,
    BitmapBgra * pDst,
    const RenderDetails * details,
    bool transpose,
    int call_number)
{

    int return_code = 0;

    //How many rows to buffer and process at a time.
    uint32_t buffer_row_count = 4; //using buffer=5 seems about 6% better than most other non-zero values. 

    //How many bytes per pixel are we scaling?
    uint32_t scaling_bpp = (pSrc->bpp == 4 && !pSrc->alpha_meaningful) ? 3 : pSrc->bpp;

    BitmapFloat * buf = CreateBitmapFloat(pSrc->w, buffer_row_count, scaling_bpp, false); /*Handle errors */  if (buf == NULL)  { return_code = -1; goto cleanup; }
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
        if (details->apply_color_matrix && call_number == 2) { ApplyColorMatrix(r, buf, row_count); }

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


static int RenderWrapper1D(
    const Renderer * r, 
    BitmapBgra * pSrc,
    BitmapBgra * pDst,
    const RenderDetails * details,
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

int PerformRender(Renderer * r) 
{
    CompleteHalving(r);
    bool skip_last_transpose = r->details->post_transpose;

    /*
    //We can optimize certain code paths - later, if needed

    bool scaling_required = (r->canvas != NULL) && (r->details->post_transpose ? (r->canvas->w != r->source->h || r->canvas->h != r->source->w) :
        (r->canvas->h != r->source->h || r->canvas->w != r->source->w));

    
    bool someTranspositionRequired = r->details->sharpen_percent_goal > 0 ||
        skip_last_transpose ||
        r->details->kernel_a_radius > 0 ||
        r->details->kernel_b_radius > 0 ||
        scaling_required;

    if (!someTranspositionRequired && canvas == NULL){
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
    r->transposed = CreateBitmapBgra(r->source->h, r->canvas == NULL ? r->source->w : (skip_last_transpose ? r->canvas->h : r->canvas->w), false, r->source->bpp);
    if (r->transposed == NULL) { return -2;  }
    r->transposed->compositing_mode = Replace_self;
    //p->Stop("allocate temp image(sy x dx)", true, false);

    //Don't composite if we're working in-place
    if (r->canvas == NULL){
        r->source->compositing_mode = Replace_self;
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

    BitmapBgra * finalDest = r->canvas == NULL ? r->source : r->canvas;

    //Apply kernels, color matrix, scale,  (transpose?) and (compose?)

    if (RenderWrapper1D(r, r->transposed, finalDest, r->details, !skip_last_transpose, 2)){
        return -6;
    }

    //p->Stop("Render", true, false);
    //GC::KeepAlive(wbSource);
    //GC::KeepAlive(wbCanvas);
}

InterpolationDetails * CreateInterpolation(InterpolationFilter filter)
{
    switch (filter) {
        case Filter_Linear:
        case Filter_Triangle:
            return CreateCustom(1, 1, filter_triangle);
        case Filter_Lanczos2:
            return CreateCustom(2, 1, filter_sinc_2);
        case Filter_Lanczos3: //Note - not a 3 lobed function - truncated to 2
            return CreateCustom(3, 1, filter_sinc_2);
        case Filter_Lanczos2Sharp:
            return CreateCustom(2, 0.9549963639785485, filter_sinc_2); 
        case Filter_Lanczos3Sharp://Note - not a 3 lobed function - truncated to 2
            return CreateCustom(3, 0.9812505644269356, filter_sinc_2);
        
        //Hermite and BSpline no negative weights
        case Filter_CubicBSpline:
            return CreateBicubicCustom(2, 1, 1, 0);

        case Filter_Lanczos2Windowed:
            return CreateCustom(2, 1, filter_sinc_windowed);
        case Filter_Lanczos3Windowed:
            return CreateCustom(3, 1, filter_sinc_windowed);
        case Filter_Lanczos2SharpWindowed:
            return CreateCustom(2, 0.9549963639785485, filter_sinc_windowed);
        case Filter_Lanczos3SharpWindowed:
            return CreateCustom(3, 0.9812505644269356, filter_sinc_windowed);

           
        case Filter_CubicFast:
            return CreateCustom(1, 1, filter_bicubic_fast);
        case Filter_Cubic:
            return CreateBicubicCustom(2, 1, 0,1);
        case Filter_CatmullRom:
            return CreateBicubicCustom(2, 1, 0, 0.5);
        case Filter_CatmullRomFast:
            return CreateBicubicCustom(1, 1, 0, 0.5);
        case Filter_CatmullRomFastSharp:
            return CreateBicubicCustom(1, 13.0 / 16.0, 0, 0.5);
        case Filter_Mitchell:
            return CreateBicubicCustom(2, 7.0 / 8.0, 1.0 / 3.0, 1.0 / 3.0);
        case Filter_Robidoux:
            return CreateBicubicCustom(2, 1. / 1.1685777620836932,
                0.37821575509399867, 0.31089212245300067);
        case Filter_RobidouxSharp:
            return CreateBicubicCustom(2, 1. / 1.105822933719019,
                0.2620145123990142, 0.3689927438004929);
        case Filter_Hermite:
            return CreateBicubicCustom(1, 1, 0, 0);
        case Filter_Box:
            return CreateCustom(0.5, 1, filter_box);

    }
    return NULL;
}
