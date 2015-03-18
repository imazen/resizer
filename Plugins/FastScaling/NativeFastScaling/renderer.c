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

typedef struct RendererStruct {
    RenderDetails * details;
    BitmapBgra * source;
    bool destroy_source;
    BitmapBgra * canvas;
    BitmapBgra * transposed;
    ProfilingLog * log;
} Renderer;


InterpolationDetails* create_interpolation_details()
{
    InterpolationDetails* d = (InterpolationDetails*)calloc(1, sizeof(InterpolationDetails));
    if (d == NULL) return NULL;
    d->blur = 1;
    d->window = 2;
    d->p1 = d->q1 = 0;
    d->p2 = d->q2 = d->p3 = d->q3 = d->q4 = 1;
    d->sharpen_percent_goal = 0;
    return d;
}

RenderDetails * create_render_details()
{
    RenderDetails * d = (RenderDetails *)calloc(1, sizeof(RenderDetails));
    if (d == NULL) return NULL;
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

static void DestroyRenderDetails(RenderDetails * d){
    if (d->interpolation != NULL) free(d->interpolation);

    if (d->kernel_a != NULL) free_convolution_kernel(d->kernel_a);

    if (d->kernel_b != NULL) free_convolution_kernel(d->kernel_b);

    free(d);
}



/*
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
*/

void destroy_renderer(Renderer * r)
{
    if (r == NULL) return;
    if (r->destroy_source) {
        destroy_bitmap_bgra(r->source);
    }
    r->source = NULL;
    destroy_bitmap_bgra(r->transposed);
    r->transposed = NULL;
    r->canvas = NULL;
    if (r->details != NULL) {
        DestroyRenderDetails(r->details);
        r->details = NULL;
    }
    if (r->log != NULL){
        free(r->log->log);
        free(r->log);
        r->log = NULL;
    }
    free(r);
}



inline void profiler_start(const Renderer * r, const char * name, bool allow_recursion){
    ProfilingEntry * current = &(r->log->log[r->log->count]);
    r->log->count++;
    if (r->log->count >= r->log->capacity) return;

    current->time =get_high_precision_ticks();
    current->name = name;
    current->flags = allow_recursion ? Profiling_start_allow_recursion : Profiling_start;
}
inline void profiler_stop(const Renderer * r, const char * name, bool assert_started, bool stop_children){
    ProfilingEntry * current = &(r->log->log[r->log->count]);
    r->log->count++;
    if (r->log->count >= r->log->capacity) return;

    current->time =get_high_precision_ticks();
    current->name = name;
    current->flags = assert_started ? Profiling_stop_assert_started : Profiling_stop;
    if (stop_children) {current->flags = current->flags | Profiling_stop_children; }
}


ProfilingLog * access_profiling_log(Renderer * r){
    return r->log;
}

static ProfilingLog * create_profiling_log(uint32_t capacity){
    ProfilingLog *  p = (ProfilingLog *)calloc(1, sizeof(ProfilingLog));
    if (p == NULL) return NULL;

    ProfilingEntry * log = (ProfilingEntry *)malloc(sizeof(ProfilingEntry) * capacity);
    if (log == NULL){
        free(p);
        return NULL;
    }
    p->log = log;
    p->capacity = capacity;
    p->count = 0;
    return p;
}

Renderer * create_renderer_in_place(BitmapBgra * editInPlace, RenderDetails * details)
{
    if (details->post_transpose) return NULL; //We can't transpose in place.
    Renderer * r = (Renderer *)calloc(1, sizeof(Renderer));
    if (r == NULL) return NULL;
    r->source = editInPlace;
    r->destroy_source = false;
    r->details = details;
    if (details->enable_profiling){
        r->log = create_profiling_log((r->source->h + r->source->w) * 20 + 50);
    }
    return r;
}

Renderer * create_renderer(BitmapBgra * source, BitmapBgra * canvas, RenderDetails * details)
{
    Renderer * r = (Renderer *)calloc(1, sizeof(Renderer));
    if (r == NULL) return NULL;
    r->source = source;
    r->canvas = canvas;
    r->destroy_source = false;
    r->details = details;
    if (details->enable_profiling){
        r->log = create_profiling_log((r->source->w + r->source->h + r->canvas->w + r->canvas->h) * 20 + 50);
    }
    else{
        r->log = NULL;
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

static int complete_halving(Context * context, Renderer * r)
{
    int divisor = r->details->halving_divisor;
    if (divisor <= 1){
        return 0;
    }
    int halved_width = (int)(r->source->w / divisor);
    int halved_height = (int)(r->source->h / divisor);

    prof_start(r, "CompleteHalving", false);
    r->details->halving_divisor = 0; //Don't halve twice

    if (r->source->can_reuse_space){
        int result = HalveInPlace(r->source, divisor);
        if (result == 0) return -101;
    }
    else {
        prof_start(r,"create temp image for halving", false);
        BitmapBgra * tmp_im = create_bitmap_bgra(context, halved_width, halved_height, true, r->source->fmt);
        if (tmp_im == NULL) return -102;
        prof_stop(r,"create temp image for halving", true, false);

        int result = Halve(r->source, tmp_im, divisor);
        if (result == 0) {
            return -103;
        }
        tmp_im->alpha_meaningful = r->source->alpha_meaningful;

        if (r->destroy_source) {
            destroy_bitmap_bgra(r->source);
        }
        r->source = tmp_im;
        r->destroy_source = true; //Cleanup tmp_im
    }
    return 0;
    prof_stop(r,"CompleteHalving", true, false);
}


static int ApplyConvolutionsFloat1D(const Renderer * r, BitmapFloat * img, const uint32_t from_row, const uint32_t row_count, double sharpening_applied)
{
    prof_start(r,"convolve kernel a",  false);
    if (r->details->kernel_a != NULL && ConvolveBgraFloatInPlace(img, r->details->kernel_a, img->channels, from_row, row_count)){
        return -3;
    }
    prof_stop(r,"convolve kernel a", true, false);

    prof_start(r,"convolve kernel b",  false);
    if (r->details->kernel_b != NULL && ConvolveBgraFloatInPlace(img, r->details->kernel_b, img->channels, from_row, row_count)){
        return -3;
    }
    prof_stop(r,"convolve kernel b", true, false);

    if (r->details->sharpen_percent_goal > sharpening_applied + 0.01){
        prof_start(r,"SharpenBgraFloatRowsInPlace", false);
        SharpenBgraFloatRowsInPlace(img, from_row, row_count, r->details->sharpen_percent_goal - sharpening_applied);
        prof_stop(r,"SharpenBgraFloatRowsInPlace", true, false);
    }
    return 0;
}

static void ApplyColorMatrix(const Renderer * r, BitmapFloat * img, const uint32_t row_count)
{
    prof_start(r,"apply_color_matrix_float", false);
    apply_color_matrix_float(img, 0, row_count, r->details->color_matrix);
    prof_stop(r,"apply_color_matrix_float", true, false);
}

void Context_initialize(Context * context) 
{
    context->file = NULL;
    context->line = -1;
    context->reason = No_Error;
}

void Context_set_last_error(Context * context, StatusCode code, const char * file, int line)
{
    context->reason = code;
    context->file = file;
    context->line = line;
}

const char * TheStatus = "The almight status has happened";
static const char * status_code_to_string(StatusCode code) 
{
    return TheStatus;
}

const char * Context_last_error_message(Context * context, char * buffer, size_t buffer_size)
{
    snprintf(buffer, buffer_size, "Error in file: %s line: %d reason: %s", context->file, context->line, status_code_to_string(context->reason));
    return buffer;
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
    BitmapPixelFormat scaling_format = (pSrc->fmt == Bgra32 && !pSrc->alpha_meaningful) ? Bgr24 : pSrc->fmt;

    prof_start(r,"contributions_calc", false);

    contrib = contributions_calc(to_count, from_count, details->interpolation);  /*Handle errors */ if (contrib == NULL) { return_code = -1; goto cleanup; }
    prof_stop(r,"contributions_calc", true, false);


    prof_start(r,"create_bitmap_float (buffers)", false);

    source_buf = create_bitmap_float(from_count, buffer_row_count, scaling_format, false); /*Handle errors */  if (source_buf == NULL)  { return_code = -1; goto cleanup; }

    dest_buf = create_bitmap_float(to_count, buffer_row_count, scaling_format, false);  /*Handle errors */   if (source_buf == NULL)  { return_code = -1; goto cleanup; }
    source_buf->alpha_meaningful = pSrc->alpha_meaningful;
    dest_buf->alpha_meaningful = source_buf->alpha_meaningful;

    source_buf->alpha_premultiplied = source_buf->channels == 4;
    dest_buf->alpha_premultiplied = source_buf->alpha_premultiplied;

    prof_stop(r,"create_bitmap_float (buffers)", true, false);


    /* Scale each set of lines */
    for (uint32_t source_start_row = 0; source_start_row < pSrc->h; source_start_row += buffer_row_count) {
        const uint32_t row_count = MIN(pSrc->h - source_start_row, buffer_row_count);

        prof_start(r,"convert_srgb_to_linear", false);
        if (convert_srgb_to_linear(pSrc, source_start_row, source_buf, 0, row_count)){
            return_code = -2; goto cleanup;
        }
        prof_stop(r,"convert_srgb_to_linear", true, false);

        prof_start(r,"ScaleBgraFloatRows", false);
        ScaleBgraFloatRows(source_buf, 0, dest_buf, 0, row_count, contrib->ContribRow);
        prof_stop(r,"ScaleBgraFloatRows", true, false);


        if (ApplyConvolutionsFloat1D(r, dest_buf, 0, row_count, contrib->percent_negative)){
            return_code = -3; goto cleanup;
        }
        if (details->apply_color_matrix && call_number == 2) { ApplyColorMatrix(r, dest_buf, row_count); }

        prof_start(r,"pivoting_composite_linear_over_srgb", false);
        if (pivoting_composite_linear_over_srgb(dest_buf, 0, pDst, source_start_row, row_count, transpose)){
            return_code = -4; goto cleanup;
        }
        prof_stop(r,"pivoting_composite_linear_over_srgb", true, false);

    }
    //sRGB sharpening
    //Color matrix


cleanup:
    //p->Start("Free Contributions,FloatBuffers", false);

    if (contrib != NULL) contributions_free(contrib);

    if (source_buf != NULL) destroy_bitmap_float(source_buf);
    if (dest_buf != NULL) destroy_bitmap_float(dest_buf);
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
     BitmapPixelFormat scaling_format = (pSrc->fmt == Bgra32 && !pSrc->alpha_meaningful) ? Bgr24 : pSrc->fmt;


     BitmapFloat * buf = create_bitmap_float (pSrc->w, buffer_row_count, scaling_format, false); /*Handle errors */  if (buf == NULL)  { return_code = -1; goto cleanup; }
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
    if (buf != NULL) destroy_bitmap_float(buf);
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

int perform_render(Context * context, Renderer * r)
{
    prof_start(r,"perform_render", false);
    complete_halving(context, r);
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
    r->transposed = create_bitmap_bgra(
	context, 
	r->source->h, 
	r->canvas == NULL ? r->source->w : (skip_last_transpose ? r->canvas->h : r->canvas->w), 
	false, 
	r->source->fmt);
    if (r->transposed == NULL) { return -2;  }
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

    prof_stop(r,"perform_render", true, false);
    //p->Stop("Render", true, false);
    //GC::KeepAlive(wbSource);
    //GC::KeepAlive(wbCanvas);
    return 0; // is this correct?
}

InterpolationDetails * create_interpolation(InterpolationFilter filter)
{
    switch (filter) {
        case Filter_Linear:
        case Filter_Triangle:
            return create_custom(1, 1, filter_triangle);
        case Filter_Lanczos2:
            return create_custom(2, 1, filter_sinc_2);
        case Filter_Lanczos3: //Note - not a 3 lobed function - truncated to 2
            return create_custom(3, 1, filter_sinc_2);
        case Filter_Lanczos2Sharp:
            return create_custom(2, 0.9549963639785485, filter_sinc_2);
        case Filter_Lanczos3Sharp://Note - not a 3 lobed function - truncated to 2
            return create_custom(3, 0.9812505644269356, filter_sinc_2);

        //Hermite and BSpline no negative weights
        case Filter_CubicBSpline:
            return create_bicubic_custom(2, 1, 1, 0);

        case Filter_Lanczos2Windowed:
            return create_custom(2, 1, filter_sinc_windowed);
        case Filter_Lanczos3Windowed:
            return create_custom(3, 1, filter_sinc_windowed);
        case Filter_Lanczos2SharpWindowed:
            return create_custom(2, 0.9549963639785485, filter_sinc_windowed);
        case Filter_Lanczos3SharpWindowed:
            return create_custom(3, 0.9812505644269356, filter_sinc_windowed);


        case Filter_CubicFast:
            return create_custom(1, 1, filter_bicubic_fast);
        case Filter_Cubic:
            return create_bicubic_custom(2, 1, 0,1);
        case Filter_CatmullRom:
            return create_bicubic_custom(2, 1, 0, 0.5);
        case Filter_CatmullRomFast:
            return create_bicubic_custom(1, 1, 0, 0.5);
        case Filter_CatmullRomFastSharp:
            return create_bicubic_custom(1, 13.0 / 16.0, 0, 0.5);
        case Filter_Mitchell:
            return create_bicubic_custom(2, 7.0 / 8.0, 1.0 / 3.0, 1.0 / 3.0);
        case Filter_Robidoux:
            return create_bicubic_custom(2, 1. / 1.1685777620836932,
                0.37821575509399867, 0.31089212245300067);
        case Filter_RobidouxSharp:
            return create_bicubic_custom(2, 1. / 1.105822933719019,
                0.2620145123990142, 0.3689927438004929);
        case Filter_Hermite:
            return create_bicubic_custom(1, 1, 0, 0);
        case Filter_Box:
            return create_custom(0.5, 1, filter_box);

    }
    return NULL;
}

void destroy_interpolation_details(InterpolationDetails * doomed)
{
    free(doomed);
}


#ifndef _TIMERS_IMPLEMENTED
#define _TIMERS_IMPLEMENTED
#ifdef _WIN32
    #define STRICT
    #define WIN32_LEAN_AND_MEAN
    #include <windows.h>
    #include <winbase.h>
    inline int64_t get_high_precision_ticks(void){
        LARGE_INTEGER val;
        QueryPerformanceCounter(&val);
        return val.QuadPart;
    }
    int64_t get_profiler_frequency(void){
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

    int64_t get_profiler_frequency(void){
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
