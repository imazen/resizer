/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */

#pragma once
#include "shared.h"
#include "fastscaling.h"

#ifdef __cplusplus
extern "C" {
#endif

static void derive_cubic_coefficients(double B, double C, InterpolationDetails * out){
    double bx2 = B + B;
    out->p1 = 1.0 - (1.0 / 3.0)*B;
    out->p2 = -3.0 + bx2 + C;
    out->p3 = 2.0 - 1.5*B - C;
    out->q1 = (4.0 / 3.0)*B + 4.0*C;
    out->q2 = -8.0*C - bx2;
    out->q3 = B + 5.0*C;
    out->q4 = (-1.0 / 6.0)*B - C;
}




static LineContribType *  ContributionsAlloc(const uint32_t line_length, const uint32_t windows_size)
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

#ifdef __cplusplus
}
#endif
