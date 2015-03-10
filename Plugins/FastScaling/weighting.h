#include "Stdafx.h"
#pragma once
#pragma unmanaged
#include "shared.h"

static void derive_cubic_coefficients(double B, double C, InterpolationDetailsPtr out){
    double bx2 = B + B;
    out->p1 = 1.0 - (1.0 / 3.0)*B;
    out->p2 = -3.0 + bx2 + C;
    out->p3 = 2.0 - 1.5*B - C;
    out->q1 = (4.0 / 3.0)*B + 4.0*C;
    out->q2 = -8.0*C - bx2;
    out->q3 = B + 5.0*C;
    out->q4 = (-1.0 / 6.0)*B - C;
}


static inline double filter_flex_cubic(const InterpolationDetailsPtr d, double x)
{
    const double t = (double)fabs(x) / d->blur;

    if (t < 1.0){
        return (d->p1 + t * (t* (d->p2 + t*d->p3)));
    }
    if (t < 2.0){
        return(d->q1 + t*(d->q2 + t* (d->q3 + t*d->q4)));
    }
    return(0.0);
}
static inline double filter_bicubic_fast(const InterpolationDetailsPtr d, const double t)
{
    const double abs_t = (double)fabs(t) / d->blur;
    const double abs_t_sq = abs_t * abs_t;
    if (abs_t<1) return 1 - 2 * abs_t_sq + abs_t_sq*abs_t;
    if (abs_t<2) return  (4 - 8 * abs_t + 5 * abs_t_sq - abs_t_sq*abs_t);
    return 0;
}


static inline double filter_sinc_2(const InterpolationDetailsPtr d, double t)
{
    const double abs_t = (double)fabs(t) / d->blur;
    if (abs_t == 0) { return 1; } //Avoid division by zero
    if (abs_t > 2){ return 0; }
    const double a = abs_t * IR_PI;
    return sin(a) / a;
}

static inline double filter_box(const InterpolationDetailsPtr d, double t)
{

    const double x = t / d->blur;
    return (x >= -1 * d->window && x < d->window) ? 1 : 0;
}

static inline double filter_triangle(const InterpolationDetailsPtr d, double t)
{
    const double x = (double)fabs(t) / d->blur;
    if (x < 1.0)
        return(1.0 - x);
    return(0.0);
}


static inline double filter_sinc_windowed(const InterpolationDetailsPtr d, double t)
{
    const double x = t / d->blur;
    const double abs_t = (double)fabs(x);
    if (abs_t == 0) { return 1; } //Avoid division by zero
    if (abs_t > d->window){ return 0; }
    return d->window * sin(IR_PI * x / d->window) * sin(x * IR_PI) / (IR_PI * IR_PI * x * x);
}



static InterpolationDetailsPtr CreateBicubicCustom(double window, double blur, double B, double C){
    InterpolationDetailsPtr d = CreateInterpolationDetails();
    d->blur = blur;
    derive_cubic_coefficients(B, C, d);
    d->filter = filter_flex_cubic;
    d->window = window;
    return d;
}
static InterpolationDetailsPtr CreateCustom(double window, double blur, detailed_interpolation_method filter){
    InterpolationDetailsPtr d = CreateInterpolationDetails();
    d->blur = blur;
    d->filter = filter;
    d->window = window;
    return d;
}

static InterpolationDetailsPtr CreateInterpolation(InterpolationFilter filter){
    switch (filter){
        case Filter_Linear:
        case InterpolationFilter::Filter_Triangle:
            return CreateCustom(1, 1, filter_triangle);
        case InterpolationFilter::Filter_Lanczos2:
            return CreateCustom(2, 1, filter_sinc_2);
        case InterpolationFilter::Filter_Lanczos3: //Note - not a 3 lobed function - truncated to 2
            return CreateCustom(3, 1, filter_sinc_2);
        case InterpolationFilter::Filter_Lanczos2Sharp:
            return CreateCustom(2, 0.9549963639785485, filter_sinc_2); 
        case InterpolationFilter::Filter_Lanczos3Sharp://Note - not a 3 lobed function - truncated to 2
            return CreateCustom(3, 0.9812505644269356, filter_sinc_2);
        
        //Hermite and BSpline no negative weights
        case Filter_CubicBSpline:
            return CreateBicubicCustom(2, 1, 1, 0);

        case InterpolationFilter::Filter_Lanczos2Windowed:
            return CreateCustom(2, 1, filter_sinc_windowed);
        case InterpolationFilter::Filter_Lanczos3Windowed:
            return CreateCustom(3, 1, filter_sinc_windowed);
        case InterpolationFilter::Filter_Lanczos2SharpWindowed:
            return CreateCustom(2, 0.9549963639785485, filter_sinc_windowed);
        case InterpolationFilter::Filter_Lanczos3SharpWindowed:
            return CreateCustom(3, 0.9812505644269356, filter_sinc_windowed);

           
        case Filter_CubicFast:
            return CreateCustom(1, 1, filter_bicubic_fast);
        case Filter_Cubic:
            return CreateBicubicCustom(2, 0.5, 1, 0);
        case Filter_CatmullRom:
            return CreateBicubicCustom(2, 0.5, 0, 0.5);
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







static inline LineContribType *  ContributionsAlloc(const uint32_t line_length, const uint32_t windows_size)
{
    LineContribType *res = (LineContribType *)malloc(sizeof(LineContribType));
    if (!res) {
        return NULL;
    }
    res->WindowSize = windows_size;
    res->LineLength = line_length;
    res->ContribRow = (ContributionType *)malloc(line_length * sizeof(ContributionType));
    if (!res->ContribRow) {
        return NULL;
    }

    float *allWeights = (float *)calloc(windows_size * line_length, sizeof(float));
    if (!allWeights) {
        return NULL;
    }

    for (uint32_t i = 0; i < line_length; i++)
        res->ContribRow[i].Weights = allWeights + (i * windows_size);

    return res;
}

static inline void ContributionsFree(LineContribType * p)
{
    free(p->ContribRow[0].Weights);
    free(p->ContribRow);
    free(p);
}

#define TONY 0.00001

static double percent_negative_weight(const InterpolationDetailsPtr details){
    const int samples = 50;
    double step = details->window / (double)samples;

    
    double last_height = details->filter(details, -step);
    double positive_area = 0;
    double negative_area = 0;
    for (int i = 0; i <= samples + 2; i++){
        const double height = details->filter(details, i * step);
        const double area = (height + last_height) / 2.0 * step;
        last_height = height;
        if (area > 0) positive_area += area;
        else negative_area -= area;
    }
    return negative_area / positive_area;
}


static LineContribType *ContributionsCalc(const uint32_t line_size, const uint32_t src_size, const InterpolationDetailsPtr details)
{
    const double sharpen_ratio =  percent_negative_weight(details);
    const double desired_sharpen_ratio = details->sharpen_percent_goal / 100.0;
    const double extra_negative_weight = sharpen_ratio > 0 && desired_sharpen_ratio > 0 ?
        (desired_sharpen_ratio + sharpen_ratio) / sharpen_ratio :
        0;


    const double scale_factor = (double)line_size / (double)src_size;
    const double downscale_factor = MIN(1.0, scale_factor);
    const double half_source_window = details->window * 0.5 / downscale_factor;

    const uint32_t allocated_window_size = (int)ceil(2 * (half_source_window - TONY)) + 1;
    uint32_t u, ix;
    LineContribType *res = ContributionsAlloc(line_size, allocated_window_size);

    double negative_area = 0;
    double positive_area = 0;

    for (u = 0; u < line_size; u++) {
        const double center_src_pixel = ((double)u + 0.5) / scale_factor - 0.5;

        const int left_edge = (int)ceil(center_src_pixel - half_source_window - 0.5 + TONY);
        const int right_edge = (int)floor(center_src_pixel + half_source_window + 0.5 - TONY);

        const uint32_t left_src_pixel = MAX(0, left_edge);
        const uint32_t right_src_pixel = MIN(right_edge, (int)src_size - 1);

        double total_weight = 0.0;

        const uint32_t source_pixel_count = right_src_pixel - left_src_pixel + 1;

        if (source_pixel_count > allocated_window_size){
            ContributionsFree(res);
            exit(200);
            return NULL;
        }

        res->ContribRow[u].Left = left_src_pixel;
        res->ContribRow[u].Right = right_src_pixel;


        float *weights = res->ContribRow[u].Weights;

        //commented: additional weight for edges (doesn't seem to be too effective)
        //for (ix = left_edge; ix <= right_edge; ix++) {
        for (ix = left_src_pixel; ix <= right_src_pixel; ix++) {
            int tx = ix - left_src_pixel;
            //int tx = MIN(MAX(ix, left_src_pixel), right_src_pixel) - left_src_pixel;
            double add = (*details->filter)(details, downscale_factor * ((double)ix - center_src_pixel));
            if (add < 0 && extra_negative_weight != 0){
                add *= extra_negative_weight;
            }
            weights[tx] = (float)add;
            total_weight += add;
        }

        if (total_weight <= TONY) {
            ContributionsFree(res);
            return NULL;
        }

        
        const float total_factor = (float)(1.0f / total_weight);
        for (ix = 0; ix < source_pixel_count; ix++) {
            weights[ix] *= total_factor;
            if (weights[ix] < 0){
                negative_area -= weights[ix];
            }
            else{
                positive_area += weights[ix];
            }
        }
    }
    res->percent_negative = negative_area / positive_area;
    return res;
}


