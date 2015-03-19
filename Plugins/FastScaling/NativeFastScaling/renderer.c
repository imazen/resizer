/*
* Copyright (c) Imazen LLC.
* No part of this project, including this file, may be copied, modified,
* propagated, or distributed except as permitted in COPYRIGHT.txt.
* Licensed under the GNU Affero General Public License, Version 3.0.
* Commercial licenses available at http://imageresizing.net/
*/

#ifdef _MSC_VER
#pragma unmanaged
#pragma warning(disable : 4996)
#define snprintf _snprintf
#endif


#include "fastscaling_private.h"
#include <stdio.h>
#include <assert.h>

typedef struct RendererStruct {
    RenderDetails * details;
    BitmapBgra * source;
    bool destroy_source;
    BitmapBgra * canvas;
    BitmapBgra * transposed;
} Renderer;


InterpolationDetails* InterpolationDetails_create(Context * context)
{
    InterpolationDetails* d = CONTEXT_calloc_array(context, 1, InterpolationDetails);
    if (d == NULL) {
        CONTEXT_error(context, Out_of_memory);
        return NULL;
    }
    d->blur = 1;
    d->window = 2;
    d->p1 = d->q1 = 0;
    d->p2 = d->q2 = d->p3 = d->q3 = d->q4 = 1;
    d->sharpen_percent_goal = 0;
    return d;
}

RenderDetails * RenderDetails_create(Context * context)
{
    RenderDetails * d = CONTEXT_calloc_array(context, 1, RenderDetails);
    if (d == NULL) {
        CONTEXT_error(context, Out_of_memory);
        return NULL;
    }
    for (int i = 0; i < 5; i++) {
        d->color_matrix[i] = &(d->color_matrix_data[i * 5]);
    }
    d->enable_profiling=false;
    d->interpolate_last_percent = 3;
    d->halve_only_when_common_factor = true;
    d->minimum_sample_window_to_interposharpen = 1.5;
    d->apply_color_matrix = false;
    return d;
}

static void RenderDetails_destroy(Context * context, RenderDetails * d){
    if (d != NULL){
        CONTEXT_free(context, d->interpolation);
        ConvolutionKernel_destroy(context, d->kernel_a);
        ConvolutionKernel_destroy(context, d->kernel_b);
    }
    CONTEXT_free(context, d);
}

static int Renderer_determine_divisor(Renderer * r)
{
    if (r->canvas == NULL) return 0;

    int width = r->details->post_transpose ? r->canvas->h : r->canvas->w;
    int height = r->details->post_transpose ? r->canvas->w : r->canvas->h;


    double divisor_max = fmin((double)r->source->w / (double)width,
        (double)r->source->h / (double)height);

    divisor_max = divisor_max / r->details->interpolate_last_percent;

    int divisor = (int)floor(divisor_max);
    if (r->details->halve_only_when_common_factor) {
        while (divisor > 0 && ((r->source->h % divisor != 0) || (r->source->w % divisor == 0))) {
            divisor--;
        }
    }
    return max(1, divisor);
}

void Renderer_destroy(Context * context, Renderer * r)
{
    if (r == NULL) return;
    if (r->destroy_source) {
        BitmapBgra_destroy(context, r->source);
    }
    r->source = NULL;
    BitmapBgra_destroy(context, r->transposed);
    r->transposed = NULL;
    r->canvas = NULL;
    RenderDetails_destroy(context, r->details);
    r->details = NULL;

    CONTEXT_free(context, r);
}

Renderer * Renderer_create_in_place(Context * context, BitmapBgra * editInPlace, RenderDetails * details)
{
    if (details->post_transpose) {
        CONTEXT_error(context, Transpose_not_permitted_in_place);
        return NULL;
    }
    Renderer * r = CONTEXT_calloc_array(context, 1, Renderer);
    if (r == NULL) {
        CONTEXT_error(context, Out_of_memory);
        return NULL;
    }
    if (details->enable_profiling){
        uint32_t default_capacity = (r->source->h + r->source->w) * 20 + 5;
        if (!Context_enable_profiling(context, default_capacity)){
            CONTEXT_free(context, r);
            return NULL;
        }
    }
    r->source = editInPlace;
    r->destroy_source = false;
    r->details = details;
    return r;
}

Renderer * Renderer_create(Context * context, BitmapBgra * source, BitmapBgra * canvas, RenderDetails * details)
{
    Renderer * r = CONTEXT_calloc_array(context, 1, Renderer);
    if (r == NULL) {
        CONTEXT_error(context, Out_of_memory);
        return NULL;
    }
    r->source = source;
    r->canvas = canvas;
    r->destroy_source = false;
    r->details = details;
    if (details->enable_profiling){
        uint32_t default_capacity = (r->source->w + r->source->h + r->canvas->w + r->canvas->h) * 20 + 50;
        if (!Context_enable_profiling(context, default_capacity)){
            CONTEXT_free(context, r);
            return NULL;
        }
    }
    if (r->details->halving_divisor == 0) {
        r->details->halving_divisor = Renderer_determine_divisor(r);
    }
    return r;
}

/*
static void SimpleRenderInPlace(void)
{
    //against source:

    //fliph
    //flipv

    //color matrix (srgb)

}
*/


// TODO: find better name
static bool HalveInTempImage(Context * context, Renderer * r, int divisor) 
{
    bool result = true;
    prof_start(context,"create temp image for halving", false);
    int halved_width = (int)(r->source->w / divisor);
    int halved_height = (int)(r->source->h / divisor);
    BitmapBgra * tmp_im = BitmapBgra_create(context, halved_width, halved_height, true, r->source->fmt);
    if (tmp_im == NULL){
        //TODO: we should probably log the caller as well
        return false;
    }
    // from here we have a temp image
    prof_stop(context,"create temp image for halving", true, false);

    if (!Halve(context, r->source, tmp_im, divisor)){
        // we cannot return here, or tmp_im will leak
        result = false;
    }
    tmp_im->alpha_meaningful = r->source->alpha_meaningful;
    
    if (r->destroy_source) {
        BitmapBgra_destroy(context,r->source);
    }
    r->source = tmp_im;
    r->destroy_source = true; //Cleanup tmp_im
    return result;
}

static bool Renderer_complete_halving(Context * context, Renderer * r)
{
    int divisor = r->details->halving_divisor;
    if (divisor <= 1) {
        return true;
    }
    bool result = true;
    prof_start(context, "CompleteHalving", false);
    r->details->halving_divisor = 0; //Don't halve twice
    if (r->source->can_reuse_space){
        if (!HalveInPlace(context, r->source, divisor)){
            result = false;
        }
    }
    else {
        result = HalveInTempImage(context, r, divisor);
    }
    prof_stop(context,"CompleteHalving", true, false);
    return result;
}


static bool ApplyConvolutionsFloat1D(Context * context, const Renderer * r, BitmapFloat * img, const uint32_t from_row, const uint32_t row_count, double sharpening_applied)
{
    prof_start(context,"convolve kernel a",  false);
    if (r->details->kernel_a != NULL && !BitmapFloat_convolve_rows(context, img, r->details->kernel_a, img->channels, from_row, row_count)){
        //Additional stack frame could be useful here
        return false;
    }
    prof_stop(context,"convolve kernel a", true, false);

    prof_start(context,"convolve kernel b",  false);
    if (r->details->kernel_b != NULL && !BitmapFloat_convolve_rows(context, img,  r->details->kernel_b, img->channels, from_row, row_count)){
        //Additional stack frame could be useful here
        return false;
    }
    prof_stop(context,"convolve kernel b", true, false);

    if (r->details->sharpen_percent_goal > sharpening_applied + 0.01){
        prof_start(context,"SharpenBgraFloatRowsInPlace", false);
        if (!BitmapFloat_sharpen_rows(context, img, from_row, row_count, r->details->sharpen_percent_goal - sharpening_applied)){
            return false;
        }
        prof_stop(context,"SharpenBgraFloatRowsInPlace", true, false);
    }
    return true;
}

static bool ApplyColorMatrix(Context * context, const Renderer * r, BitmapFloat * img, const uint32_t row_count)
{
    prof_start(context,"apply_color_matrix_float", false);
    bool b= BitmapFloat_apply_color_matrix(context, img, 0, row_count, r->details->color_matrix);
    prof_stop(context,"apply_color_matrix_float", true, false);
    return b;
}


static bool ScaleAndRender1D(Context * context, const Renderer * r,
    BitmapBgra * pSrc,
    BitmapBgra * pDst,
    const RenderDetails * details,
    bool transpose,
    int call_number)
{
    LineContributions * contrib = NULL;
    BitmapFloat * source_buf = NULL;
    BitmapFloat * dest_buf = NULL;

    uint32_t from_count = pSrc->w;
    uint32_t to_count = transpose ? pDst->h : pDst->w;

    bool success = true;

    if (details->interpolation->window == 0){
        CONTEXT_error(context, Invalid_argument);
        return false;
    }


    //How many rows to buffer and process at a time.
    const uint32_t buffer_row_count = 4; //using buffer=5 seems about 6% better than most other non-zero values.

    //How many bytes per pixel are we scaling?
    BitmapPixelFormat scaling_format = (pSrc->fmt == Bgra32 && !pSrc->alpha_meaningful) ? Bgr24 : pSrc->fmt;

    prof_start(context,"contributions_calc", false);

    contrib = LineContributions_create(context, to_count, from_count, details->interpolation);
    if (contrib == NULL) {
        success = false;
        goto cleanup;
    }
    prof_stop(context,"contributions_calc", true, false);


    prof_start(context,"create_bitmap_float (buffers)", false);

    source_buf = BitmapFloat_create(context, from_count, buffer_row_count, scaling_format, false);
    if (source_buf == NULL) {
        success = false;
        goto cleanup;
    }
    dest_buf = BitmapFloat_create(context, to_count, buffer_row_count, scaling_format, false);
    if (dest_buf == NULL) {
        success = false;
        goto cleanup;
    }
    source_buf->alpha_meaningful = pSrc->alpha_meaningful;
    dest_buf->alpha_meaningful = source_buf->alpha_meaningful;

    source_buf->alpha_premultiplied = source_buf->channels == 4;
    dest_buf->alpha_premultiplied = source_buf->alpha_premultiplied;

    prof_stop(context,"create_bitmap_float (buffers)", true, false);


    /* Scale each set of lines */
    for (uint32_t source_start_row = 0; source_start_row < pSrc->h; source_start_row += buffer_row_count) {
        const uint32_t row_count = umin(pSrc->h - source_start_row, buffer_row_count);

        prof_start(context,"convert_srgb_to_linear", false);
        if (!BitmapBgra_convert_srgb_to_linear(context,pSrc, source_start_row, source_buf, 0, row_count)){
            success=false; goto cleanup;
        }
        prof_stop(context,"convert_srgb_to_linear", true, false);

        prof_start(context,"ScaleBgraFloatRows", false);
        if (!BitmapFloat_scale_rows(context, source_buf, 0, dest_buf, 0, row_count, contrib->ContribRow)){
            success=false; goto cleanup;
        }
        prof_stop(context,"ScaleBgraFloatRows", true, false);


        if (!ApplyConvolutionsFloat1D(context, r, dest_buf, 0, row_count, contrib->percent_negative)){
            success=false; goto cleanup;
        }
        if (details->apply_color_matrix && call_number == 2) {
            if (!ApplyColorMatrix(context, r, dest_buf, row_count)){
                success=false; goto cleanup;
            }
        }

        prof_start(context,"pivoting_composite_linear_over_srgb", false);
        if (!BitmapFloat_pivoting_composite_linear_over_srgb(context, dest_buf, 0, pDst, source_start_row, row_count, transpose)){
            success=false; goto cleanup;
        }
        prof_stop(context,"pivoting_composite_linear_over_srgb", true, false);

    }
    //sRGB sharpening
    //Color matrix


cleanup:
    //p->Start("Free Contributions,FloatBuffers", false);

    if (contrib != NULL) LineContributions_destroy(context, contrib);

    if (source_buf != NULL) BitmapFloat_destroy(context, source_buf);
    if (dest_buf != NULL) BitmapFloat_destroy(context, dest_buf);
    ///p->Stop("Free Contributions,FloatBuffers", true, false);

    return success;
}



static bool Render1D(Context * context,
    const Renderer * r,
    BitmapBgra * pSrc,
    BitmapBgra * pDst,
    const RenderDetails * details,
    bool transpose,
    int call_number)
{

    bool success= true;
    //How many rows to buffer and process at a time.
    uint32_t buffer_row_count = 4; //using buffer=5 seems about 6% better than most other non-zero values.

    //How many bytes per pixel are we scaling?
    BitmapPixelFormat scaling_format = (pSrc->fmt == Bgra32 && !pSrc->alpha_meaningful) ? Bgr24 : pSrc->fmt;


    BitmapFloat * buf = BitmapFloat_create(context,pSrc->w, buffer_row_count, scaling_format, false);
    if (buf == NULL)  {
        return false;
    }
    buf->alpha_meaningful = pSrc->alpha_meaningful;
    buf->alpha_premultiplied = buf->channels == 4;



    /* Scale each set of lines */
    for (uint32_t source_start_row = 0; source_start_row < pSrc->h; source_start_row += buffer_row_count) {
        const uint32_t row_count = umin(pSrc->h - source_start_row, buffer_row_count);

        if (!BitmapBgra_convert_srgb_to_linear(context, pSrc, source_start_row, buf, 0, row_count)){
            success=false; goto cleanup;
        }
        if (!ApplyConvolutionsFloat1D(context, r, buf, 0, row_count, 0)){
            success=false; goto cleanup;
        }
        if (details->apply_color_matrix && call_number == 2) {
            if (!ApplyColorMatrix(context, r, buf, row_count)){
                success=false; goto cleanup;
            }
        }

        if (!BitmapFloat_pivoting_composite_linear_over_srgb(context, buf, 0, pDst, source_start_row, row_count, transpose)){
            success=false; goto cleanup;
        }
    }
    //sRGB sharpening
    //Color matrix


cleanup:
    BitmapFloat_destroy(context,buf);
    return success;
}


static bool RenderWrapper1D(
    Context * context,
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
        return Render1D(context, r, pSrc, pDst, details, transpose, call_number);
    }
    else{
        return ScaleAndRender1D(context, r, pSrc, pDst, details, transpose, call_number);
    }
    // }
    // finally{
    // p->Stop(name, true, true);
    //}
}

bool Renderer_perform_render(Context * context, Renderer * r)
{
    prof_start(context,"perform_render", false);
    if (!Renderer_complete_halving(context, r)) {
       return false;
    }
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
    if (vflip_source && !BitmapBgra_flip_vertical(context,r->source)){
        return false;
    }

    //Create transposition byffer
    //p->Start("allocate temp image(sy x dx)", false);

    /* Scale horizontally  */
    r->transposed = BitmapBgra_create(
        context,
        r->source->h,
        r->canvas == NULL ? r->source->w : (skip_last_transpose ? r->canvas->h : r->canvas->w),
        false,
        r->source->fmt);

    if (r->transposed == NULL) {
        return false;
    }
    r->transposed->compositing_mode = Replace_self;
    //p->Stop("allocate temp image(sy x dx)", true, false);

    //Don't composite if we're working in-place
    if (r->canvas == NULL){
        r->source->compositing_mode = Replace_self;
    }
    //Unsharpen when interpolating if we can
    if (r->details->interpolation != NULL &&
        r->details->sharpen_percent_goal > 0 &&
        r->details->minimum_sample_window_to_interposharpen <= r->details->interpolation->window){

        r->details->interpolation->sharpen_percent_goal = r->details->sharpen_percent_goal;
    }


    //Apply kernels, scale, and transpose
    if (!RenderWrapper1D(context, r, r->source, r->transposed, r->details, true, 1)){
        return false;
    }

    //Apply flip to transposed
    if (vflip_transposed && !BitmapBgra_flip_vertical(context,r->transposed)){
        return false;
    }
    //Restore the source bitmap if we flipped it in place incorrectly
    if (vflip_source && r->source->pixels_readonly && !BitmapBgra_flip_vertical(context,r->source)){
        return false;
    }

    BitmapBgra * finalDest = r->canvas == NULL ? r->source : r->canvas;

    //Apply kernels, color matrix, scale,  (transpose?) and (compose?)

    if (!RenderWrapper1D(context, r, r->transposed, finalDest, r->details, !skip_last_transpose, 2)){
        return false;
    }

    prof_stop(context,"perform_render", true, false);
    //p->Stop("Render", true, false);
    //GC::KeepAlive(wbSource);
    //GC::KeepAlive(wbCanvas);
    return true;; // is this correct?
}

InterpolationDetails * InterpolationDetails_create_from(Context * context, InterpolationFilter filter)
{
    switch (filter) {
        case Filter_Linear:
        case Filter_Triangle:
            return InterpolationDetails_create_custom(context, 1, 1, filter_triangle);
        case Filter_Lanczos2:
            return InterpolationDetails_create_custom(context, 2, 1, filter_sinc_2);
        case Filter_Lanczos3: //Note - not a 3 lobed function - truncated to 2
            return InterpolationDetails_create_custom(context, 3, 1, filter_sinc_2);
        case Filter_Lanczos2Sharp:
            return InterpolationDetails_create_custom(context, 2, 0.9549963639785485, filter_sinc_2);
        case Filter_Lanczos3Sharp://Note - not a 3 lobed function - truncated to 2
            return InterpolationDetails_create_custom(context, 3, 0.9812505644269356, filter_sinc_2);

        //Hermite and BSpline no negative weights
        case Filter_CubicBSpline:
            return InterpolationDetails_create_bicubic_custom(context, 2, 1, 1, 0);

        case Filter_Lanczos2Windowed:
            return InterpolationDetails_create_custom(context, 2, 1, filter_sinc_windowed);
        case Filter_Lanczos3Windowed:
            return InterpolationDetails_create_custom(context, 3, 1, filter_sinc_windowed);
        case Filter_Lanczos2SharpWindowed:
            return InterpolationDetails_create_custom(context, 2, 0.9549963639785485, filter_sinc_windowed);
        case Filter_Lanczos3SharpWindowed:
            return InterpolationDetails_create_custom(context, 3, 0.9812505644269356, filter_sinc_windowed);


        case Filter_CubicFast:
            return InterpolationDetails_create_custom(context, 1, 1, filter_bicubic_fast);
        case Filter_Cubic:
            return InterpolationDetails_create_bicubic_custom(context, 2, 1, 0,1);
        case Filter_CatmullRom:
            return InterpolationDetails_create_bicubic_custom(context, 2, 1, 0, 0.5);
        case Filter_CatmullRomFast:
            return InterpolationDetails_create_bicubic_custom(context, 1, 1, 0, 0.5);
        case Filter_CatmullRomFastSharp:
            return InterpolationDetails_create_bicubic_custom(context, 1, 13.0 / 16.0, 0, 0.5);
        case Filter_Mitchell:
            return InterpolationDetails_create_bicubic_custom(context, 2, 7.0 / 8.0, 1.0 / 3.0, 1.0 / 3.0);
        case Filter_Robidoux:
            return InterpolationDetails_create_bicubic_custom(context, 2, 1. / 1.1685777620836932,
                0.37821575509399867, 0.31089212245300067);
        case Filter_RobidouxSharp:
            return InterpolationDetails_create_bicubic_custom(context, 2, 1. / 1.105822933719019,
                0.2620145123990142, 0.3689927438004929);
        case Filter_Hermite:
            return InterpolationDetails_create_bicubic_custom(context, 1, 1, 0, 0);
        case Filter_Box:
            return InterpolationDetails_create_custom(context, 0.5, 1, filter_box);

    }
    CONTEXT_error(context, Invalid_interpolation_filter);
    return NULL;
}

void InterpolationDetails_destroy(Context * context, InterpolationDetails * details)
{
    CONTEXT_free(context, details);
}


#ifndef _TIMERS_IMPLEMENTED
#define _TIMERS_IMPLEMENTED
#ifdef _WIN32
    #define STRICT
    #define WIN32_LEAN_AND_MEAN
    #include <windows.h>
    #include <winbase.h>
    int64_t get_high_precision_ticks(void){
        LARGE_INTEGER val;
        QueryPerformanceCounter(&val);
        return val.QuadPart;
    }
    int64_t Context_get_profiler_ticks_per_second(Context * context){
        LARGE_INTEGER val;
        QueryPerformanceFrequency(&val);
        return val.QuadPart;
    }
#else
    #include <sys/time.h>
    #if defined(_POSIX_VERSION)
    #if defined(_POSIX_TIMERS) && (_POSIX_TIMERS > 0)
    #if defined(CLOCK_MONOTONIC_PRECISE)
            /* BSD. --------------------------------------------- */
            #define PROFILER_CLOCK_ID id = CLOCK_MONOTONIC_PRECISE;
    #elif defined(CLOCK_MONOTONIC_RAW)
            /* Linux. ------------------------------------------- */
            #define PROFILER_CLOCK_ID id = CLOCK_MONOTONIC_RAW;
    #elif defined(CLOCK_HIGHRES)
            /* Solaris. ----------------------------------------- */
            #define PROFILER_CLOCK_ID id = CLOCK_HIGHRES;
    #elif defined(CLOCK_MONOTONIC)
            /* AIX, BSD, Linux, POSIX, Solaris. ----------------- */
            #define PROFILER_CLOCK_ID id = CLOCK_MONOTONIC;
    #elif defined(CLOCK_REALTIME)
            /* AIX, BSD, HP-UX, Linux, POSIX. ------------------- */
            #define PROFILER_CLOCK_ID id = CLOCK_REALTIME;
    #endif
    #endif
    #endif


    inline int64_t get_high_precision_ticks(void){
        #ifdef PROFILER_CLOCK_ID
            timespec ts;
            if (clock_gettime(PROFILER_CLOCK_ID, &ts) != 0){
                return -1;
            }
            return ts->tv_sec * 1000000 +  ts->tv_nsec;
        #else
            struct timeval tm;
            if (gettimeofday( &tm, NULL) != 0){
                return -1;
            }
            return tm.tv_sec * 1000000 + tm.tv_usec;
        #endif
    }

    int64_t Context_get_profiler_ticks_per_second(Context * context){
        #ifdef PROFILER_CLOCK_ID
            timespec ts;
            if (clock_getres(PROFILER_CLOCK_ID, &ts) != 0){
                return -1;
            }
            return ts->tv_nsec;
        #else
            return 1000000;
        #endif
    }

#endif
#endif
