// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

#include <stdio.h>

#include <string.h>

#include "fastscaling_private.h"


#ifdef _MSC_VER


#include <stdarg.h>


#pragma warning(disable : 4996)
#define snprintf _snprintf

#endif // _MSC_VER


bool test_contrib_windows(Context * context, char *msg)
{
    int bad = -1;
    LineContributions *lct = 0;

    // assumes included edge cases

    InterpolationDetails* cubicFast = InterpolationDetails_create_from(context, InterpolationFilter::Filter_CubicFast);

    unsigned int from_w = 6;
    unsigned int to_w = 3;
    unsigned int corr36[3][2] = { { 0, 1 }, { 2, 3 }, { 4, 5 } };
    lct = LineContributions_create(context, to_w, from_w, cubicFast);

    for (uint32_t i = 0; i < lct->LineLength; i++)
        if (lct->ContribRow[i].Left != (int)corr36[i][0]) { bad = i; break; }
        else if (lct->ContribRow[i].Right != (int)corr36[i][1]) { bad = i; break; }

    if (bad != -1) {
    	snprintf(msg, 255, "at 6->3 invalid value (%d; %d) at %d, expected (%d; %d)",
    		 lct->ContribRow[bad].Left,
    		 lct->ContribRow[bad].Right,
    		 bad, corr36[bad][0], corr36[bad][1]);
    	LineContributions_destroy(context, lct);
    	return false;
    }
    LineContributions_destroy(context, lct);

    from_w = 6;
    to_w = 4;
    unsigned int corr46[4][2] = { { 0, 1 }, { 1, 2 }, { 3, 4 }, { 4, 5 } };
    lct = LineContributions_create(context, to_w, from_w, cubicFast);
    InterpolationDetails_destroy(context, cubicFast);

    for (uint32_t i = 0; i < lct->LineLength; i++)
	if (lct->ContribRow[i].Left != (int)corr46[i][0]) { bad = i; break; }
	else if (lct->ContribRow[i].Right != (int)corr46[i][1]) { bad = i; break; }

    if (bad != -1) {
    	snprintf(msg, 255, "at 6->4 invalid value (%d; %d) at %d, expected (%d; %d)",
    		 lct->ContribRow[bad].Left,
    		 lct->ContribRow[bad].Right,
    		 bad, corr46[bad][0], corr46[bad][1]);
    	LineContributions_destroy(context, lct);
    	return false;
    }
    LineContributions_destroy(context, lct);
    return true;
}

bool function_bounded(Context * context, InterpolationDetails* details, char *msg, double input_start_value, double stop_at_abs, double input_step, double result_low_threshold, double result_high_threshold, const char *name)
{
    double input_value = input_start_value;

    if (fabs(input_value) >= fabs(stop_at_abs))
        return true;

    double result_value = (*details->filter)(details, input_value);

    if (result_value < result_low_threshold) {
    	snprintf(msg + strlen(msg), 255 - strlen(msg), "value %.4f is below %.4f at x=%.4f (%s)", result_value, result_low_threshold, input_value, name);
    	return false;
    }
    else if (result_value > result_high_threshold) {
    	snprintf(msg + strlen(msg), 255 - strlen(msg), "value %.4f exceeds %.4f at x=%.4f (%s)", result_value, result_high_threshold, input_value,name);
    	return false;
    }

    return function_bounded(context, details, msg, input_value + input_step, stop_at_abs, input_step, result_low_threshold, result_high_threshold, name);
}

bool function_bounded_bi(Context * context, InterpolationDetails* details, char *msg, double input_start_value, double stop_at_abs, double input_step, double result_low_threshold, double result_high_threshold, const char* name)
{
    return function_bounded(context, details, msg, input_start_value, stop_at_abs, input_step, result_low_threshold, result_high_threshold, name) &&
        function_bounded(context, details, msg, input_start_value * -1.0f, stop_at_abs, input_step * -1.0f, result_low_threshold, result_high_threshold, name);
}

bool test_details(Context * context, InterpolationDetails* details, char *msg, double expected_first_crossing, double expected_second_crossing, double expected_near0, double near0_threshold, double expected_end)
{
    double top = (*details->filter)(details, 0);

    // Verify peak is at x = 0
    if (!function_bounded_bi(context, details, msg, 0, expected_end, 0.05, -500, top, "should peak at x=0")) return false;

    // Verify we drop below a certain threshold between expected_near0 and expected_second_crossing or expected_end
    if (!function_bounded_bi(context, details, msg, expected_near0, expected_second_crossing > 0 ? expected_second_crossing : expected_end, 0.05, -500, near0_threshold, "should near 0")) return false;

    //Ensure ended at expected_end
    if (!function_bounded_bi(context, details, msg, expected_end, expected_end + 1, 0.05, -0.0001f, 0.0001f, "should end at expected_end")) return false;

    if (expected_first_crossing != 0 && expected_second_crossing != 0){
        //Ensure everything between the two crossings is negative
        if (!function_bounded_bi(context, details, msg, expected_first_crossing + 0.05, expected_second_crossing - 0.05, 0.05, -500, -0.0001f, "should be negative between crossing 1 and 2")) return false;

        //Ensure everything between second crossing and end is positive - if significant
        if (expected_end > expected_second_crossing + 0.1){
            if (!function_bounded_bi(context, details, msg, expected_second_crossing + 0.05, expected_end - 0.02, 0.02, 0, 500, "should be positive between crossing 2 and expected_end")) return false;

        }
    }
    else{
        //Ensure everything is non-negative
        if (!function_bounded_bi(context, details, msg, expected_near0, expected_end, 0.05, -0.0001, 500, "this function should only produce positive weights")) return false;

    }


    return true;
}

char * test_filter(Context * context, InterpolationFilter filter, char *msg, double expected_first_crossing, double expected_second_crossing, double expected_near0, double near0_threshold, double expected_end){
    InterpolationDetails* details = InterpolationDetails_create_from(context, filter);
    snprintf(msg,255, "Filter=(%d) ", filter);
    bool result = test_details(context, details, msg, expected_first_crossing, expected_second_crossing, expected_near0, near0_threshold, expected_end);
    InterpolationDetails_destroy(context, details);
    if (!result) return msg;
    else return nullptr;
}

bool test_weight_distrib(Context * context, char *msg)
{
    //These have window = 1, and shouldnt' have negative values. They should also end at 1
    if (!test_filter(context, InterpolationFilter::Filter_Hermite, msg, 0, 0, 0.99, 0.08, 1)) return false;
    //Also called a linear filter
    if (!test_filter(context, InterpolationFilter::Filter_Triangle, msg, 0, 0, 0.99, 0.08, 1)) return false;
    //Box should only return a value from -0.5..0.5
    if (!test_filter(context, InterpolationFilter::Filter_Box, msg, 0, 0, 0.51, 0.001, 0.51)) return false;



    //These should go negative between x=1 and x=2, but should end at x=2
    if (!test_filter(context, InterpolationFilter::Filter_CatmullRom, msg, 1, 2, 1, 0.08, 2)) return false;
    if (!test_filter(context, InterpolationFilter::Filter_CubicFast, msg, 1, 2, 1, 0.08, 2)) return false;
    if (!test_filter(context, InterpolationFilter::Filter_Cubic, msg, 1, 2, 1, 0.08, 2)) return false;

    //BSpline is a smoothing filter, always positive
    if (!test_filter(context, InterpolationFilter::Filter_CubicBSpline, msg, 0, 0, 1.75, 0.08, 2)) return false;


    // if (!test_filter(InterpolationFilter::Filter_Mitchell, msg, 1, 1.74, 1, 0.08, 1.75)) return false;
    // if (!test_filter(InterpolationFilter::Filter_Robidoux, msg, 1, 1.6, 1, 0.08, 1.75)) return false;
    // if (!test_filter(InterpolationFilter::Filter_RobidouxSharp, msg, 1, 1.8, 1, 0.08, 1.8)) return false;


    //Sinc filters. These have second crossings.
    if (!test_filter(context, InterpolationFilter::Filter_Lanczos2, msg, 1, 2, 1, 0.08, 2)) return false;
    //if (!test_filter(InterpolationFilter::Filter_Lanczos2Sharp, msg, 0.954, 1.86, 1, 0.08, 2)) return false;

    //These should be negative between x=1 and x=2, positive between 2 and 3, but should end at 3

    if (!test_filter(context, InterpolationFilter::Filter_Lanczos3, msg, 1, 2, 1, 0.1, 3)) return false;
    if (!test_filter(context, InterpolationFilter::Filter_Lanczos3Sharp, msg, 0.98, 1.9625, 1, 0.1, 2.943)) return false;

    ///
    if (!test_filter(context, InterpolationFilter::Filter_Lanczos2Windowed, msg, 1, 2, 1, 0.08, 2)) return false;

    if (!test_filter(context, InterpolationFilter::Filter_Lanczos2SharpWindowed, msg, 0.954, 1.86, 1, 0.08, 2)) return false;

    //These should be negative between x=1 and x=2, positive between 2 and 3, but should end at 3

    if (!test_filter(context, InterpolationFilter::Filter_Lanczos3Windowed, msg, 1, 2, 1, 0.1, 3)) return false;


    if (!test_filter(context, InterpolationFilter::Filter_Lanczos3SharpWindowed, msg, 0.98, 1.9625, 1, 0.1, 2.943)) return false;
    return true;
}

InterpolationDetails*  sample_filter(Context * context, InterpolationFilter filter, double x_from, double x_to, double *buffer, int samples){
    InterpolationDetails* details = InterpolationDetails_create_from(context, filter);
    if (details == NULL) return NULL;
    for (int i = 0; i < samples; i++){
        double x = (x_to - x_from) * ((double)i / (double)samples) + x_from;
        buffer[i] = details->filter(details, x);
    }
    return details;
}
