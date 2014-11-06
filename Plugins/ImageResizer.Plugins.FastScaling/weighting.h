#include "Stdafx.h"
#include "shared.h"
#pragma once
#pragma unmanaged

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
    x *= 2;
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
    if (abs_t<2) return 4 - 8 * abs_t + 5 * abs_t_sq - abs_t_sq*abs_t;
    return 0;
}

#define IR_PI  double (3.1415926535897932384626433832795)
#define IR_SINC(value) (value == 0 ? 1 : sin(value * IR_PI) / (value * IR_PI))

static inline double filter_lanczos(const InterpolationDetailsPtr d, double t)
{
    t *= 3;
    const double width = d->filter_var_a;

    const double abs_t = (double)fabs(t) / d->blur;
    if (abs_t < width)	{
        return (IR_SINC(abs_t) * IR_SINC(abs_t / width));
    }
    return 0;
}





static InterpolationDetailsPtr CreateBicubicCustom(double window, double blur, double B, double C){
    InterpolationDetailsPtr d = CreateInterpolationDetails();
    d->blur = blur;
    derive_cubic_coefficients(B, C, d);
    d->filter = filter_flex_cubic;
    d->window = window;
    return d;
}

static InterpolationDetailsPtr DetailsLanczosCustom(double window, double blur){
    InterpolationDetailsPtr d = CreateInterpolationDetails();
    d->blur = blur;
    d->filter = filter_lanczos;
    d->window = window;
    d->filter_var_a = 3;
    return d;
}
static InterpolationDetailsPtr DetailsLanczos(){
    return DetailsLanczosCustom(0.5, 1);
}

static InterpolationDetailsPtr DetailsOriginal(){
    InterpolationDetailsPtr d = CreateInterpolationDetails();
    d->blur = 1;
    d->filter = filter_bicubic_fast;
    d->window = 0.5;
    return d;
}

static InterpolationDetailsPtr DetailsDefault(){
    return CreateBicubicCustom(0.5, 1, 1, 0);
}

static InterpolationDetailsPtr DetailsGeneralCubic(){
    return CreateBicubicCustom(0.5, 2, 1, 0);
}
static InterpolationDetailsPtr DetailsCatmullRom(){
    return CreateBicubicCustom(0.5, 2, 0, 0.5);
}
static InterpolationDetailsPtr DetailsMitchell(){
    return CreateBicubicCustom(0.5, 8.0 / 7.0, 1. / 3., 1. / 3.);
}
static InterpolationDetailsPtr DetailsRobidoux(){
    return CreateBicubicCustom(0.5, 1.1685777620836932,
        0.37821575509399867, 0.31089212245300067);
}

static InterpolationDetailsPtr DetailsRobidouxSharp(){
    return CreateBicubicCustom(0.5, 1.105822933719019,
        0.2620145123990142, 0.3689927438004929);
}
static InterpolationDetailsPtr DetailsHermite(){
    return CreateBicubicCustom(0.5, 2, 1, 0);
}






static inline LineContribType *  ContributionsAlloc(unsigned int line_length, unsigned int windows_size)
{
    unsigned int u = 0;
    LineContribType *res;

    res = (LineContribType *)malloc(sizeof(LineContribType));
    if (!res) {
        return NULL;
    }
    res->WindowSize = windows_size;
    res->LineLength = line_length;
    res->ContribRow = (ContributionType *)malloc(line_length * sizeof(ContributionType));


    float *allWeights = (float *)malloc(windows_size * line_length * sizeof(float));

    for (int i = 0; i < line_length; i++)
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

static inline LineContribType *ContributionsCalc(unsigned int line_size, unsigned int src_size, const InterpolationDetailsPtr details)
{
    double width_d;
    double scale_d = (double)line_size / (double)src_size;
    double scale_f_d = 1.0;
    const double filter_width_d = details->window;
    int windows_size;
    unsigned int u;
    LineContribType *res;

    if (scale_d < 1.0) {
        width_d = filter_width_d / scale_d;
        scale_f_d = scale_d;
    }
    else {
        width_d = filter_width_d;
    }

    windows_size = (int)ceil(2 * (width_d - TONY)) + 1;
    res = ContributionsAlloc(line_size, windows_size);

    for (u = 0; u < line_size; u++) {
        const double dCenter = ((double)u + 0.5) / scale_d - 0.5;
        /* get the significant edge points affecting the pixel */
        register int iLeft = MAX(0, (int)ceil(dCenter - width_d - TONY));
        int iRight = MIN(MAX((int)floor(dCenter + width_d + TONY), iLeft), (int)src_size - 1);
        double dTotalWeight = 0.0;
        int iSrc;

        res->ContribRow[u].Left = iLeft;
        res->ContribRow[u].Right = iRight;

        for (iSrc = iLeft; iSrc <= iRight; iSrc++) {
            dTotalWeight += (res->ContribRow[u].Weights[iSrc - iLeft] = scale_f_d * (*details->filter)(details, scale_f_d * (dCenter - (double)iSrc)));
        }

        if (dTotalWeight < 0.0) {
            ContributionsFree(res);
            return NULL;
        }

        if (dTotalWeight > 0.0) {
            for (iSrc = iLeft; iSrc <= iRight; iSrc++) {
                res->ContribRow[u].Weights[iSrc - iLeft] /= dTotalWeight;
            }
        }

    }
    return res;
}


